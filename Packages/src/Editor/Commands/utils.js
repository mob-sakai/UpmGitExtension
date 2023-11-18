const fs = require("fs");

const hashCode = str => {
  return Array.from(str)
    .reduce((s, c) => Math.imul(31, s) + c.charCodeAt(0) | 0, 0)
}

const mkdirSyncRecrusive = postPath => {
  postPath.split("/").reduce((acc, item) => {
    const path = item ? (acc ? [acc, item].join("/") : item) : "";
    if (path && !fs.existsSync(path)) {
      console.log(`  Make dir: ${path}`);
      try {
        fs.mkdirSync(path);
      } catch (e) {
      }
    }
    return path;
  }, "");
};

const shouldFetch = filePath => {
  // Check previous result (keep: 2 min).
  console.log(`\n>> Check previous result (keep: 5 min)`);
  if (fs.existsSync(filePath)) {
    const diff = Date.now() - fs.statSync(filePath).mtime.getTime();
    const diffMinutes = Math.floor(diff / 1000 / 60);
    console.log(`last modified: ${diffMinutes} minute(s) ago`);
    if (diffMinutes < 5) {
      console.warn(`keep previous result.`);
      return false;
    }
  }

  // Check .lock file (timeout: 5 min).
  console.log(`\n>> Check .lock file (timeout: 5 min)`);
  const lockFile = `${filePath}.lock`;
  if (fs.existsSync(lockFile)) {
    const diff = Date.now() - fs.statSync(lockFile).mtime.getTime();
    const diffMinutes = Math.floor(diff / 1000 / 60);
    console.log(`last modified: ${diffMinutes} minute(s) ago`);
    if (diffMinutes < 5) {
      console.warn("previous task is running.");
      return false;
    } else {
      console.warn("previous task is timeout.");
      fs.unlinkSync(lockFile);
    }
  }

  return true;
};

const touch = (filePath) => {
  const now = new Date();
  fs.utimesSync(filePath, now, now);
}

const lock = (filePath) => {
  const lockFile = `${filePath}.lock`;
  console.log(`\n>> Lock file cleated: ${lockFile}`);

  fs.writeFileSync(lockFile, "", "utf-8");
}

const unlock = (filePath) => {
  const lockFile = `${filePath}.lock`;
  console.log(`\n>> Lock file will be removed: ${lockFile}`);

  if (fs.existsSync(lockFile)) fs.unlinkSync(lockFile);
}

const loadJs = (filePath) => {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch (e) {
    return {};
  }
}

module.exports = { hashCode, mkdirSyncRecrusive, shouldFetch, lock, unlock, loadJs, touch };
