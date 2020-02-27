// nodePath:
//   Windows: C:\Program Files\Unity\Editor\Data\Tools\nodejs\node.exe
//   OSX: /Applications/Unity/Unity.app/Contents/Tools/nodejs/bin/node
//   Linux: /opt/unity/Editor/Data/Tools/nodejs/bin/node
//
// Example:
//   {nodePath} getAvailableVersions.js {packageName} {repoUrl}
const { execSync } = require("child_process");
const fs = require("fs");
const path = require("path");

// Input
console.log("\nInput");
const packageName = process.argv[2];
const repoUrl = process.argv[3];
const unityVersion = process.argv[4];

console.log(`  repoUrl: ${repoUrl}`);
console.log(`  packageName: ${packageName}`);
console.log(`  unityVersion: ${unityVersion}`);

if (!repoUrl || !packageName || !unityVersion) process.exit(1);

const hashCode = str => {
  return Array.from(str)
    .reduce((s, c) => Math.imul(31, s) + c.charCodeAt(0) | 0, 0)
}

const id = hashCode(`${packageName}@${repoUrl}`);
const repoDir = `Library/UGE/packages/${id}}`;
const resultDir = `Library/UGE/results`;
const outputFile = path.resolve(`${resultDir}/${id}.json`);
const lockFile = ".lock";

const mkdirSyncRecrusive = postPath => {
  postPath.split("/").reduce((acc, item) => {
    const path = item ? (acc ? [acc, item].join("/") : item) : "";
    if (path && !fs.existsSync(path)) {
      console.log(`  Make dir: ${path}`);
      fs.mkdirSync(path);
    }
    return path;
  }, "");
};

const IsSupported = (version, unityVersion) => {
  const regVersion = /^(\d+)\.(\d+)\.(\d+)(.*)$/;
  const v = version.match(regVersion);
  const uv = unityVersion.match(regVersion);
  if (v[1] != uv[1]) return v[1] < uv[1];
  if (v[2] != uv[2]) return v[2] < uv[2];
  if (v[3] != uv[3]) return v[3] < uv[3];
  return v[4] <= uv[4];
};

const parseRef = text => {
  try {
    const regRefName = /^refs\/(tags\/|remotes\/origin\/)([^\/]+)$/;
    const ref = text.split(/\s+/)[1];
    const refName = ref.match(regRefName)[2];

    console.log(`  checkout: ${ref}`);
    execSync(`git checkout -q ${ref} -- package.json package.json.meta`);
    const p = JSON.parse(fs.readFileSync("package.json", "utf8"));

    // Check package name.
    if (packageName != "all" && p.name != packageName) {
      console.error(
        `error: '${p.name}' is not target package '${packageName}'.`
      );
      return undefined;
    }

    // Check supported Unity version.
    const unity = p.unity || "2018.3";
    const unityRelease = p.unityRelease || "0a0";
    const supportedVersion = `${unity}.${unityRelease}`;
    if (!IsSupported(supportedVersion, unityVersion)) {
      console.error(
        `error: ${unityVersion} is not supported. Supported unity versions are '>=${supportedVersion}'`
      );
      return undefined;
    }

    //
    return { packageName: p.name, repoUrl, version: p.version, refName };
  } catch (e) {
    return undefined;
  }
};

// Start task to get available packages
console.log("\n#### Start task to get available packages ####");
try {
  // Make dir and change current dir
  console.log("\n>> Make dir and change current dir ]");
  mkdirSyncRecrusive("Assets");
  mkdirSyncRecrusive(repoDir);
  mkdirSyncRecrusive(resultDir);
  process.chdir(repoDir);
  console.log(`  cwd: ${process.cwd()}`);

  // Check .lock file (timeout: 5 min)
  console.log("\n>> Check .lock file (timeout: 5 min)");
  if (fs.existsSync(lockFile)) {
    const dif = Date.now() - fs.statSync(lockFile).ctime.getTime();
    if (1000 * 60 * 5 < dif) {
      console.warn("  Previous task is timeout.");
      fs.unlinkSync(lockFile);
    } else {
      console.warn("  Previous task is running.");
      process.exit(0);
    }
  }
  fs.writeFileSync(lockFile, "", "utf-8");

  // Init git
  if (!fs.existsSync(".git")) {
    console.log(`\n>> Init git at ${repoDir}. origin is ${repoUrl}`);
    execSync("git init");
    execSync(`git remote add origin ${repoUrl.replace(/^git\+/, "")}`);
  }

  // Fast fetch repo to get refs
  console.log("\n>> Fast fetch repo to get refs");
  execSync(
    'git fetch --depth=1 -fq --prune origin "refs/tags/*:refs/tags/*" "+refs/heads/*:refs/remotes/origin/*"'
  );

  // Get valid package references
  console.log("\n>> Get valid package references");
  const versions = execSync("git show-ref", { encoding: "utf-8" })
    .split(/[\r\n]+/)
    .map(x => parseRef(x))
    .filter(x => x);

  // Output valid package references to file
  console.log(`\n>> Output valid package references to file: ${outputFile}`);
  console.log(versions);
  fs.writeFileSync(outputFile, JSON.stringify({ versions }, space = 2), "utf-8");

  console.log("\n######## Complete ########");
} finally {
  // Remove .lock file
  if (fs.existsSync(lockFile)) fs.unlinkSync(lockFile);
}
