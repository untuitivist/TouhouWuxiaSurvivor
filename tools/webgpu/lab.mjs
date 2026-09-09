import { WIDTH, HEIGHT, COUNT, STRIDE, fillSprites, summarize, comparePixels, selectBackend } from './scene.mjs';
import { createWebGPU, createWebGL } from './renderers.mjs';

const surface = document.querySelector('#surface');
const status = document.querySelector('#status');
const output = document.querySelector('#report');
const nextFrame = () => new Promise(resolve => requestAnimationFrame(resolve));
const textureResponse = await fetch('/sprite.png');
if (!textureResponse.ok) throw new Error('Texture request failed: ' + textureResponse.status);
const bitmap = await createImageBitmap(await textureResponse.blob(), { premultiplyAlpha: 'none', colorSpaceConversion: 'none', imageOrientation: 'none' });
if (bitmap.width !== 192 || bitmap.height !== 48) throw new Error('Expected the maintained four-frame fairy strip');

class RenderingLab {
    constructor() {
        this.data = new Float32Array(COUNT * STRIDE);
        this.generation = 0;
        this.animation = 0;
        this.renderer = null;
        this.state = { phase: 'starting', backend: null, history: [], scope: 'render-only; no C# combat; not a game build', width: WIDTH, height: HEIGHT, count: COUNT, strideFloats: STRIDE, bytesPerFrame: COUNT * STRIDE * 4, secure: isSecureContext, isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer, devicePixelRatio, webgpuApi: !!navigator.gpu };
    }

    describe() {
        const fallback = this.state.fallback ? ' / 已回退' : '';
        const reasons = (this.state.attempts ?? []).filter(attempt => !attempt.available).map(attempt => attempt.kind + ': ' + attempt.reason);
        status.textContent = [this.state.phase + ' / ' + (this.state.backend ?? '无可用后端') + fallback, JSON.stringify(this.state.info ?? {}), ...reasons, this.state.recoveryReason, this.state.failure].filter(Boolean).join('\n');
    }

    async select(preference, { animate = true, recoveryReason = null } = {}) {
        const generation = ++this.generation;
        cancelAnimationFrame(this.animation);
        const previous = this.renderer;
        this.renderer = null;
        previous?.dispose();
        Object.assign(this.state, { phase: 'initializing', backend: null, info: null, attempts: [], failure: null, fallback: false, recoveryReason });
        this.describe();
        const onLost = reason => {
            if (generation !== this.generation) return;
            if (this.renderer?.kind === 'webgpu') this.select('webgl2', { recoveryReason: reason });
            else { cancelAnimationFrame(this.animation); this.state.phase = 'unavailable'; this.state.recoveryReason = reason; this.describe(); }
        };
        const create = async factory => {
            const canvas = document.createElement('canvas');
            canvas.width = WIDTH;
            canvas.height = HEIGHT;
            canvas.setAttribute('aria-label', 'One thousand instanced game sprites');
            return factory(canvas, bitmap, onLost);
        };
        try {
            const selection = await selectBackend(preference, { webgpu: () => create(createWebGPU), webgl2: () => create(createWebGL) });
            if (generation !== this.generation) { selection.renderer.dispose(); return this.state; }
            this.renderer = selection.renderer;
            surface.replaceChildren(this.renderer.canvas);
            Object.assign(this.state, { phase: 'ready', backend: this.renderer.kind, info: this.renderer.info, attempts: selection.attempts, fallback: selection.fallback || !!recoveryReason, recoveryReason });
            this.state.history.push({ requested: preference, actual: this.renderer.kind, fallback: this.state.fallback, recoveryReason });
            this.describe();
            this.drawAt(0);
            if (animate) this.animate(generation);
        } catch (error) {
            if (generation !== this.generation) return this.state;
            Object.assign(this.state, { phase: 'unavailable', backend: null, attempts: error.attempts ?? [], failure: String(error.message), recoveryReason });
            surface.replaceChildren();
            this.describe();
        }
        return this.state;
    }

    drawAt(seconds) {
        if (!this.renderer) throw new Error('No initialized renderer');
        fillSprites(this.data, seconds);
        this.renderer.render(this.data);
    }

    animate(generation) {
        let start = null;
        const step = timestamp => {
            if (generation !== this.generation || this.state.phase !== 'ready') return;
            start ??= timestamp;
            try { this.drawAt(Math.max(0, (timestamp - start) / 1000)); }
            catch (error) { this.state.phase = 'unavailable'; this.state.failure = String(error); this.describe(); return; }
            this.animation = requestAnimationFrame(step);
        };
        this.animation = requestAnimationFrame(step);
    }

    async measure(kind, frames = 120) {
        if (!Number.isInteger(frames) || frames < 60 || frames > 600) throw new RangeError('Use 60 to 600 measured frames');
        await this.select(kind, { animate: false });
        if (this.renderer?.kind !== kind) throw new Error('Requested backend was not available: ' + kind);
        const renderer = this.renderer;
        let previous;
        for (let index = 0; index < 30; index++) { previous = await nextFrame(); this.drawAt(index / 60); }
        const preparation = [];
        const submission = [];
        const intervals = [];
        for (let index = 0; index < frames; index++) {
            const timestamp = await nextFrame();
            if (document.visibilityState !== 'visible' || renderer !== this.renderer) throw new Error('Measurement interrupted or hidden');
            intervals.push(timestamp - previous);
            previous = timestamp;
            const start = performance.now();
            fillSprites(this.data, index / 60);
            const prepared = performance.now();
            renderer.render(this.data);
            preparation.push(prepared - start);
            submission.push(performance.now() - prepared);
        }
        await renderer.waitIdle();
        if (renderer.errors.length) throw new Error(renderer.errors.join('; '));
        return { kind, frames, count: COUNT, drawCallsPerFrame: 1, width: WIDTH, height: HEIGHT, bytesPerFrame: this.data.byteLength, info: renderer.info, cpuPrepareMs: summarize(preparation), cpuSubmitMs: summarize(submission), animationFrameMs: summarize(intervals), renderOnlyRafHz: 1000 / summarize(intervals).mean, gpuTimeMeasured: false, fullGameFpsMeasured: false };
    }

    async pixels() {
        const checks = [];
        for (const aligned of [true, false]) {
            const fixture = fillSprites(new Float32Array(COUNT * STRIDE), 1.25);
            if (aligned) for (let offset = 0; offset < fixture.length; offset += STRIDE) {
                fixture[offset] = fixture[offset + 5] = 24;
                fixture[offset + 1] = fixture[offset + 4] = 0;
                fixture[offset + 3] = Math.round(fixture[offset + 3]);
                fixture[offset + 7] = Math.round(fixture[offset + 7]);
            }
            const pixels = [];
            for (const kind of ['webgl2', 'webgpu']) {
                await this.select(kind, { animate: false });
                if (this.renderer?.kind !== kind) throw new Error('Pixel comparison requires both actual backends');
                this.renderer.render(fixture);
                pixels.push(await this.renderer.readPixels());
                await this.renderer.waitIdle();
                if (this.renderer.errors.length) throw new Error(this.renderer.errors.join('; '));
            }
            const comparison = comparePixels(pixels[0], pixels[1]);
            this.state.lastPixelCheck = { aligned, ...comparison };
            if (comparison.visiblePixels < 10000 || comparison.differentFraction > 0.01 || comparison.meanChannelError > 0.5) throw new Error('Pixel comparison failed: ' + JSON.stringify(this.state.lastPixelCheck));
            checks.push({ aligned, ...comparison, passed: true });
        }
        output.textContent = JSON.stringify({ scope: this.state.scope, pixelChecks: checks }, null, 2);
        return checks;
    }

    async compare() {
        const results = [];
        const pixelChecks = await this.pixels();
        for (let round = 0; round < 3; round++) {
            for (const kind of round % 2 === 0 ? ['webgl2', 'webgpu'] : ['webgpu', 'webgl2']) {
                results.push({ round, ...await this.measure(kind) });
                output.textContent = JSON.stringify({ scope: this.state.scope, pixelChecks, results }, null, 2);
            }
        }
        return { scope: this.state.scope, gpuTimeMeasured: false, fullGameFpsMeasured: false, pixelChecks, results };
    }
}

const lab = new RenderingLab();
const ready = lab.select('auto');
window.webgpuLab = { ready, state: () => structuredClone(lab.state), select: (...args) => lab.select(...args), measure: (...args) => lab.measure(...args), pixels: () => lab.pixels(), compare: () => lab.compare(), forceDeviceLoss: () => lab.renderer?.forceLoss(), drawAt: seconds => lab.drawAt(seconds) };
const action = async operation => {
    const buttons = [...document.querySelectorAll('button')];
    buttons.forEach(button => { button.disabled = true; });
    try { await operation(); }
    catch (error) { output.textContent = String(error.stack || error); }
    finally { buttons.forEach(button => { button.disabled = false; }); }
};
document.querySelector('#gpu').addEventListener('click', () => action(() => lab.select('webgpu')));
document.querySelector('#gl').addEventListener('click', () => action(() => lab.select('webgl2')));
document.querySelector('#compare').addEventListener('click', () => action(() => lab.compare()));
document.querySelector('#loss').addEventListener('click', () => lab.renderer?.forceLoss());
window.addEventListener('pagehide', () => { lab.generation++; cancelAnimationFrame(lab.animation); lab.renderer?.dispose(); bitmap.close(); });
