import test from 'node:test';
import assert from 'node:assert/strict';
import { COUNT, STRIDE, WIDTH, HEIGHT, fillSprites, summarize, comparePixels, selectBackend } from './scene.mjs';

test('sprite buffers are deterministic, reusable, bounded and explicit about color', () => {
    const buffer = new Float32Array(COUNT * STRIDE);
    assert.equal(fillSprites(buffer, 2), buffer);
    assert.deepEqual(buffer, fillSprites(new Float32Array(buffer.length), 2));
    for (let index = 0; index < COUNT; index++) {
        const offset = index * STRIDE;
        assert.ok(buffer[offset + 3] > 12 && buffer[offset + 3] < WIDTH - 12);
        assert.ok(buffer[offset + 7] > 12 && buffer[offset + 7] < HEIGHT - 12);
        assert.ok(Math.abs(Math.hypot(buffer[offset], buffer[offset + 4]) - 24) < 0.00001);
        assert.ok(Number.isInteger(buffer[offset + 12]) && buffer[offset + 12] >= 0 && buffer[offset + 12] < 4);
        assert.equal(buffer[offset + 13], 0.25);
        assert.ok(buffer[offset + 11] > 0 && buffer[offset + 11] <= 1);
    }
    assert.notDeepEqual(fillSprites(new Float32Array(buffer.length), 3), buffer);
});

test('invalid workloads are rejected rather than silently reducing the budget', () => {
    for (const count of [0, -1, 1001, 1.5, NaN]) assert.throws(() => fillSprites(new Float32Array(12), 0, count));
    assert.throws(() => fillSprites(new Float32Array(12), Infinity, 1));
    assert.throws(() => fillSprites(new Float32Array(STRIDE), -0.001, 1));
    assert.throws(() => fillSprites([], 0, 1));
    assert.throws(() => fillSprites(new Float32Array(11), 0, 1));
});

test('statistics report measured latency without labeling CPU time as GPU time', () => {
    assert.deepEqual(summarize([3, 1, 2, 4]), { samples: 4, mean: 2.5, median: 2, p95: 4, max: 4 });
    for (const samples of [[], [NaN], [-1], [Infinity]]) assert.throws(() => summarize(samples));
});

test('pixel comparison detects black frames, tint and transparency mistakes', () => {
    const reference = new Uint8Array([0, 0, 0, 255, 200, 100, 50, 255]);
    assert.equal(comparePixels(reference, reference).differentPixels, 0);
    assert.equal(comparePixels(reference, reference).visiblePixels, 1);
    assert.equal(comparePixels(reference, new Uint8Array(8)).differentPixels, 2);
    assert.throws(() => comparePixels(reference, new Uint8Array(4)));
    for (const tolerance of [NaN, Infinity, -1, 256]) assert.throws(() => comparePixels(reference, reference, tolerance));
});

test('available WebGPU is selected without initializing WebGL', async () => {
    const renderer = { kind: 'webgpu' };
    const result = await selectBackend('auto', { webgpu: async () => renderer, webgl2: async () => { throw new Error('Should not initialize'); } });
    assert.equal(result.renderer, renderer);
    assert.equal(result.fallback, false);
    assert.equal(result.attempts.length, 1);
});

for (const reason of ['API unavailable', 'Adapter unavailable', 'Device request rejected', 'Pipeline validation failed']) {
    test(reason + ' retains an explicit fallback reason', async () => {
        const result = await selectBackend('webgpu', { webgpu: async () => { throw new Error(reason); }, webgl2: async () => ({ kind: 'webgl2' }) });
        assert.equal(result.fallback, true);
        assert.equal(result.attempts[0].reason, reason);
        assert.equal(result.renderer.kind, 'webgl2');
    });
}

test('explicit WebGL never requests a GPU adapter', async () => {
    const result = await selectBackend('webgl2', { webgpu: async () => { assert.fail(); }, webgl2: async () => ({ kind: 'webgl2' }) });
    assert.equal(result.attempts.length, 1);
    assert.equal(result.fallback, false);
});

test('both unavailable is a visible failure, not a fake successful fallback', async () => {
    await assert.rejects(selectBackend('auto', { webgpu: async () => { throw new Error('No GPU'); }, webgl2: async () => { throw new Error('No GL'); } }), error => error.attempts.length === 2 && /No rendering/.test(error.message));
    await assert.rejects(selectBackend('unknown', {}), RangeError);
});
