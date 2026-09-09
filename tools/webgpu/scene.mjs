export const WIDTH = 1280;
export const HEIGHT = 720;
export const COUNT = 1000;
export const STRIDE = 16;
export const TEXEL_BIAS = 1 / 1024;
export const CLEAR = [0.04, 0.07, 0.08, 1];

export function fillSprites(buffer, seconds, count = COUNT) {
    if (!(buffer instanceof Float32Array) || !Number.isInteger(count) || count < 1 || count > COUNT || buffer.length !== count * STRIDE || !Number.isFinite(seconds) || seconds < 0) throw new RangeError('Invalid sprite input');
    for (let index = 0; index < count; index++) {
        const offset = index * STRIDE;
        const phase = seconds * 0.7 + index * 0.37;
        buffer[offset] = Math.cos(phase * 0.2) * 24;
        buffer[offset + 1] = -Math.sin(phase * 0.2) * 24;
        buffer[offset + 2] = 0;
        buffer[offset + 3] = 32 + (index * 977 % 1216) + Math.sin(phase) * 12;
        buffer[offset + 4] = Math.sin(phase * 0.2) * 24;
        buffer[offset + 5] = Math.cos(phase * 0.2) * 24;
        buffer[offset + 6] = 0;
        buffer[offset + 7] = 32 + (index * 313 % 656) + Math.cos(phase * 0.9) * 12;
        buffer[offset + 8] = index % 3 === 0 ? 0.7 : 1;
        buffer[offset + 9] = index % 3 === 1 ? 0.7 : 1;
        buffer[offset + 10] = 1;
        buffer[offset + 11] = 0.65 + (index % 4) * 0.1;
        buffer[offset + 12] = (Math.floor((seconds % 0.5) * 8) + index) % 4;
        buffer[offset + 13] = 0.25;
        buffer[offset + 14] = 0;
        buffer[offset + 15] = 0;
    }
    return buffer;
}

export function summarize(values) {
    if (!values.length || values.some(value => !Number.isFinite(value) || value < 0)) throw new RangeError('Invalid samples');
    const ordered = [...values].sort((left, right) => left - right);
    return { samples: values.length, mean: values.reduce((total, value) => total + value, 0) / values.length, median: ordered[Math.floor((ordered.length - 1) / 2)], p95: ordered[Math.ceil(ordered.length * 0.95) - 1], max: ordered.at(-1) };
}

export function comparePixels(reference, candidate, tolerance = 3) {
    if (!Number.isInteger(tolerance) || tolerance < 0 || tolerance > 255) throw new RangeError('Invalid pixel tolerance');
    if (!(reference instanceof Uint8Array) || !(candidate instanceof Uint8Array) || reference.length !== candidate.length || reference.length === 0 || reference.length % 4) throw new RangeError('Pixel buffers must match');
    let differentPixels = 0;
    let maxError = 0;
    let totalError = 0;
    let visiblePixels = 0;
    for (let offset = 0; offset < reference.length; offset += 4) {
        let different = false;
        for (let channel = 0; channel < 4; channel++) {
            const error = Math.abs(reference[offset + channel] - candidate[offset + channel]);
            totalError += error;
            maxError = Math.max(maxError, error);
            different ||= error > tolerance;
        }
        if (different) differentPixels++;
        if (Math.abs(reference[offset] - reference[0]) + Math.abs(reference[offset + 1] - reference[1]) + Math.abs(reference[offset + 2] - reference[2]) > 24) visiblePixels++;
    }
    return { pixels: reference.length / 4, differentPixels, differentFraction: differentPixels / (reference.length / 4), maxError, meanChannelError: totalError / reference.length, visiblePixels };
}

export async function selectBackend(preference, factories) {
    if (!['auto', 'webgpu', 'webgl2'].includes(preference)) throw new RangeError('Unknown backend');
    const attempts = [];
    for (const kind of preference === 'webgl2' ? ['webgl2'] : ['webgpu', 'webgl2']) {
        try {
            const renderer = await factories[kind]();
            attempts.push({ kind, available: true });
            return { renderer, attempts, fallback: kind !== 'webgpu' && preference !== 'webgl2' };
        } catch (error) {
            attempts.push({ kind, available: false, reason: String(error.message || error) });
        }
    }
    const failure = new Error('No rendering backend available');
    failure.attempts = attempts;
    throw failure;
}
