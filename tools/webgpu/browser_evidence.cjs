const assert = require('node:assert/strict');

async function attachRendererEvidence(context, { fault = 'none' } = {}) {
    assert.ok(['none', 'missing-api', 'no-adapter', 'device-rejected', 'device-loss'].includes(fault));
    await context.addInitScript(({ fault }) => {
        const evidence = globalThis.__actualGpuEvidence = { contexts: [], adapters: [], deviceRequests: 0, errors: [], lost: [], webgl: null, fault, faultInjected: false };
        const getContext = HTMLCanvasElement.prototype.getContext;
        HTMLCanvasElement.prototype.getContext = function(kind, ...arguments_) {
            const result = getContext.call(this, kind, ...arguments_);
            evidence.contexts.push({ kind, canvas: this.id, available: !!result });
            if (result && kind === 'webgl2' && this.id === 'canvas') {
                const debug = result.getExtension('WEBGL_debug_renderer_info');
                evidence.webgl = { vendor: result.getParameter(debug ? debug.UNMASKED_VENDOR_WEBGL : result.VENDOR), renderer: result.getParameter(debug ? debug.UNMASKED_RENDERER_WEBGL : result.RENDERER) };
            }
            return result;
        };
        if (fault === 'missing-api') {
            Object.defineProperty(navigator, 'gpu', { value: undefined, configurable: true });
            evidence.faultInjected = true;
        }
        if (navigator.gpu) {
            const requestAdapter = navigator.gpu.requestAdapter.bind(navigator.gpu);
            navigator.gpu.requestAdapter = async options => {
                if (fault === 'no-adapter') {
                    evidence.faultInjected = true;
                    return null;
                }
                const adapter = await requestAdapter(options);
                if (!adapter) return adapter;
                evidence.adapters.push({ vendor: adapter.info?.vendor, architecture: adapter.info?.architecture, fallback: adapter.info?.isFallbackAdapter ?? adapter.isFallbackAdapter ?? null });
                const requestDevice = adapter.requestDevice.bind(adapter);
                adapter.requestDevice = async descriptor => {
                    if (fault === 'device-rejected') {
                        evidence.faultInjected = true;
                        throw new DOMException('Verification: WebGPU device creation rejected', 'NotSupportedError');
                    }
                    const device = await requestDevice(descriptor);
                    evidence.deviceRequests++;
                    globalThis.__actualGpuDevice = device;
                    device.addEventListener('uncapturederror', event => { if (evidence.errors.length < 100) evidence.errors.push(String(event.error.message)); });
                    device.lost.then(info => evidence.lost.push({ reason: info.reason, message: info.message }));
                    return device;
                };
                return adapter;
            };
        }
    }, { fault });
}

function assertRendererEvidence(evidence, expected, { allowDeviceLoss = false } = {}) {
    assert.ok(['webgpu', 'webgl2'].includes(expected));
    assert.ok(evidence.contexts.some(item => item.available && item.canvas === 'canvas' && item.kind === expected), 'Actual game canvas must use ' + expected);
    if (expected === 'webgpu') {
        assert.ok(evidence.deviceRequests > 0, 'A real WebGPU device is required');
        assert.ok(evidence.adapters.some(adapter => adapter.fallback === false), 'A hardware adapter is required');
    } else {
        assert.ok(evidence.webgl?.renderer, 'WebGL renderer identity is required');
        assert.doesNotMatch(evidence.webgl.renderer, /swiftshader|llvmpipe|software|microsoft basic render/i);
    }
    assert.deepEqual(evidence.errors, []);
    if (!allowDeviceLoss) assert.deepEqual(evidence.lost, []);
}

module.exports = { attachRendererEvidence, assertRendererEvidence };
