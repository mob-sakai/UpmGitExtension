// nodePath:
//   Windows: C:\Program Files\Unity\Editor\Data\Tools\nodejs\node.exe
//   OSX: /Applications/Unity/Unity.app/Contents/Tools/nodejs/bin/node
//   Linux: /opt/unity/Editor/Data/Tools/nodejs/bin/node
//
// Example:
//   {nodePath} fetch-packages.js {repositoryUrl}

const { execSync } = require("child_process");
const fs = require("fs");
const path = require("path");
const { hashCode, mkdirSyncRecrusive, shouldFetch, lock, unlock, loadJs, touch } = require("./utils");
const version = '2.0.0'

// Input
console.log(`cwd: ${process.cwd()}\nInput`);
const rawRepositoryUrl = process.argv[2];
const matchUrl = rawRepositoryUrl.match(/^(git\+)?(.*?)(\?path=([^#]*))?(#.*)?$/);
const repositoryUrl = matchUrl[2];
const subDir = `${matchUrl[4] || '.'}`
  .replace(/(^\/|\/$)/g, "") + '/';
const repositoryUrlWithPath = subDir !== "./"
  ? `${repositoryUrl}?path=${subDir}`
  : repositoryUrl;


const repositoryId = repositoryUrl
  .replace(/^(ssh:\/\/|http:\/\/|https:\/\/|file:\/\/|git:\/\/|git@)/, "")
  .replace(/\.git$/, "");
const subDirId = subDir != './' ? '@' + subDir.slice(0, -1) : "";
const id = (repositoryId + subDirId)
  .replace(/[:\/]/g, "~")
  .toLowerCase();
const repoDir = `Repositories/${id}`;
const resultDir = `Results-${version}`;
const outputFile = path.resolve(`${resultDir}/${id}.json`);

console.log(`  rawRepositoryUrl: ${rawRepositoryUrl}`);
console.log(`  repositoryUrl: ${repositoryUrl}`);
console.log(`  subDir: ${subDir}`);
console.log(`  id: ${id}`);
console.log(`  repoDir: ${repoDir}`);
console.log(`  outputFile: ${outputFile}`);

const parseRef = text => {
  try {
    const regRefName = /^refs\/(tags\/|remotes\/origin\/)([^\/]+)$/;
    const hash = text.split(/\s+/)[0];
    const ref = text.split(/\s+/)[1];
    const refName = ref.match(regRefName)[2];

    console.log(`  checkout: ${ref}`);
    execSync(`git checkout -q ${ref} -- ${subDir}package.json ${subDir}package.json.meta`);
    const p = JSON.parse(fs.readFileSync(`${subDir}package.json`, "utf8"));

    // Check supported Unity version.
    const unity = p.unity || "2018.3";
    const unityRelease = p.unityRelease || "0a0";

    // Parse author.
    var author = "";
    if (!p.author) { }
    else if (typeof p.author === 'string') {
      author = /^[^<(]*/.exec(p.author)[0].trim();
    }
    else if (typeof p.author === 'object') {
      author = p.author.name;
    }

    // Parse dependencies.
    var m_Dependencies = [];
    if (p.dependencies && typeof p.dependencies === 'object') {
      m_Dependencies = Object.keys(p.dependencies)
        .map(key => { return { m_Name: key, m_Version: p.dependencies[key] } });
    }

    //
    return {
      refName,
      hash,
      m_MinimumUnityVersion: `${unity}.${unityRelease}`,
      m_DisplayName: p.displayName,
      m_Description: p.description,
      m_PackageUniqueId: p.name,
      m_PackageId: `${p.name}@${repositoryUrlWithPath}#${refName}`,
      m_IsUnityPackage: false,
      m_IsInstalled: false,
      m_IsFullyFetched: true,
      m_Author: author,
      m_VersionString: p.version,
      m_Tag: 4,
      m_PackageInfo: {
        m_PackageId: `${p.name}@${repositoryUrlWithPath}#${refName}`,
        m_Name: p.name,
        m_DisplayName: p.displayName,
        m_Description: p.description,
        m_Version: p.version,
        m_Source: 5,
        m_Dependencies,
        m_Git: {
          m_Hash: hash,
          m_Revision: refName,
        },
        m_HasRepository: true,
        m_Repository: {
          m_Type: "git",
          m_Url: repositoryUrl,
          m_Revision: refName,
          m_Path: subDir,
        },
      }
    };
  } catch (e) {
    return undefined;
  }
};

// Start task to get available packages
console.log("\n#### Start task to get available packages ####");

// Make dir and change current dir
console.log("\n>> Make directory and change current working directory");
mkdirSyncRecrusive("Assets");
mkdirSyncRecrusive(repoDir);
mkdirSyncRecrusive(resultDir);
process.chdir(repoDir);
console.log(`cwd: ${process.cwd()}`);

// Check lock file and keep time
if (!shouldFetch(outputFile))
  process.exit(0);

try {
  // Lock.
  lock(outputFile);

  // Init git.
  if (!fs.existsSync(".git")) {
    console.log(`\n>> Init git at ${repoDir}. origin is ${repositoryUrl}`);
    execSync("git init");
    execSync(`git remote add origin ${repositoryUrl.replace(/^git\+/, "")}`);
  }

  // Fast fetch repo to get refs
  console.log("\n>> Fast fetch repo to get refs");
  execSync(
    'git fetch --depth=1 -fq --prune origin "refs/tags/*:refs/tags/*" "+refs/heads/*:refs/remotes/origin/*"'
  );

  // Get revisions
  console.log("\n>> Get revisions");
  const refs = execSync("git show-ref", { encoding: "utf-8" });
  console.log(refs);

  // Check previous hash.
  var hash = hashCode(version + id + refs);
  console.log(`hash: ${hash}`);
  if (hash === loadJs(outputFile).hash) {
    console.warn("previous result has same hash. update modified-timestamp.");
    touch(outputFile);
    unlock(outputFile);
    process.exit(0);
  }

  console.log("\n>> Get package version from revisions");
  const versions = refs
    .split(/[\r\n]+/)
    .map(x => parseRef(x))
    .filter(x => x);

  // Output valid package references to file
  console.log(`\n>> Output valid package (${versions.length} versions) references to file: ${outputFile}`);
  // console.dir(versions, { depth: 5 });
  fs.writeFileSync(outputFile, JSON.stringify({ id, url: rawRepositoryUrl, hash, versions }, space = 2), "utf-8");

  console.log("\n######## Complete ########");
} finally {

  // Unlock
  unlock(outputFile);
}
