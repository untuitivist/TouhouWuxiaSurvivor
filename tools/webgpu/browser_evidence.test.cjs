const test = require('node:test');
const assert = require('node:assert/strict');
const { assertRendererEvidence } = require('./browser_evidence.cjs');

function evidence(kind) {
    return { contexts: [{ kind, canvas: 'canvas', available: true }], adapters: [{ fallback: false }], deviceRequests: 1, errors: [], lost: [], webgl: { renderer: 'ANGLE NVIDIA hardware' } };
}

test('actual hardware contexts pass for the two supported backends', () => {
    for (const kind of ['webgpu', 'webgl2']) assert.doesNotThrow(() => assertRendererEvidence(evidence(kind), kind));
});

test('a capability probe is not evidence of the game renderer', () => {
    const sample = evidence('webgpu');
    sample.contexts[0].canvas = 'capability-probe';
    assert.throws(() => assertRendererEvidence(sample, 'webgpu'));
    assert.throws(() => assertRendererEvidence(evidence('webgl2'), 'webgpu'));
});

test('software backends cannot pass the hardware gate', () => {
    const gpu = evidence('webgpu');
    gpu.adapters[0].fallback = true;
    assert.throws(() => assertRendererEvidence(gpu, 'webgpu'));
    const gl = evidence('webgl2');
    gl.webgl.renderer = 'ANGLE Google SwiftShader';
    assert.throws(() => assertRendererEvidence(gl, 'webgl2'));
});

test('GPU errors and unintended loss fail; loss may only be acknowledged explicitly', () => {
    const sample = evidence('webgl2');
    sample.lost.push({ reason: 'destroyed' });
    assert.throws(() => assertRendererEvidence(sample, 'webgl2'));
    assert.doesNotThrow(() => assertRendererEvidence(sample, 'webgl2', { allowDeviceLoss: true }));
    sample.errors.push('texture failure');
    assert.throws(() => assertRendererEvidence(sample, 'webgl2', { allowDeviceLoss: true }));
});
