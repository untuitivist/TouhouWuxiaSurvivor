const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { verificationBuildPath, verificationOutputRoot } = require('./verification_paths.cjs');

test('verification defaults retain the maintained compatible pointer and output', () => {
    assert.equal(verificationBuildPath('/repo', 'artifacts/web-compatible-latest.json', {}), path.resolve('/repo', 'artifacts/web-compatible-latest.json'));
    assert.equal(verificationOutputRoot({ build: '/existing/build' }, {}), '/existing/build');
});

test('experimental selection does not require rewriting the compatible build pointer', () => {
    const environment = { TOUHOU_VERIFY_BUILD: 'artifacts/web-webgpu-latest.json', TOUHOU_VERIFY_OUTPUT: '/new/validation' };
    assert.equal(verificationBuildPath('/repo', 'artifacts/web-compatible-latest.json', environment), path.resolve('/repo', 'artifacts/web-webgpu-latest.json'));
    assert.equal(verificationOutputRoot({ build: '/existing/build' }, environment), path.resolve('/new/validation'));
});
