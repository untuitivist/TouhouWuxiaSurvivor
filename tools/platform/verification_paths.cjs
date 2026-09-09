const path = require('node:path');

function verificationBuildPath(root, fallback, environment = process.env) {
    return path.resolve(root, environment.TOUHOU_VERIFY_BUILD || fallback);
}

function verificationOutputRoot(build, environment = process.env) {
    return environment.TOUHOU_VERIFY_OUTPUT ? path.resolve(environment.TOUHOU_VERIFY_OUTPUT) : build.build;
}

module.exports = { verificationBuildPath, verificationOutputRoot };
