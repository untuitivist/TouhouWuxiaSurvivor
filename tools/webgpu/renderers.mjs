import { WIDTH, HEIGHT, COUNT, STRIDE, CLEAR, TEXEL_BIAS } from './scene.mjs';

const wgsl = `
struct VertexInput {
    @location(0) row0: vec4f,
    @location(1) row1: vec4f,
    @location(2) tint: vec4f,
    @location(3) animation: vec4f,
};
struct VertexOutput {
    @builtin(position) position: vec4f,
    @location(0) uv: vec2f,
    @location(1) tint: vec4f,
};
@group(0) @binding(0) var<uniform> viewport: vec4f;
@group(0) @binding(1) var spriteTexture: texture_2d<f32>;
@vertex fn vertex_main(input: VertexInput, @builtin(vertex_index) vertexIndex: u32) -> VertexOutput {
    let corners = array<vec2f, 6>(vec2f(0,0), vec2f(1,0), vec2f(0,1), vec2f(0,1), vec2f(1,0), vec2f(1,1));
    let corner = corners[vertexIndex];
    let local = corner - vec2f(0.5);
    let world = vec2f(dot(input.row0.xy, local) + input.row0.w, dot(input.row1.xy, local) + input.row1.w);
    var output: VertexOutput;
    output.position = vec4f(world.x / viewport.x * 2.0 - 1.0, 1.0 - world.y / viewport.y * 2.0, 0, 1);
    output.uv = vec2f((corner.x + input.animation.x) * input.animation.y, corner.y);
    output.tint = input.tint;
    return output;
}
@fragment fn fragment_main(input: VertexOutput) -> @location(0) vec4f {
    let dimensions = vec2i(textureDimensions(spriteTexture));
    let texel = clamp(vec2i(floor(input.uv * vec2f(dimensions) + vec2f(${TEXEL_BIAS}))), vec2i(0), dimensions - vec2i(1));
    return textureLoad(spriteTexture, texel, 0) * input.tint;
}
`;

const vertexSource = `#version 300 es
precision highp float;
layout(location=0) in vec4 row0;
layout(location=1) in vec4 row1;
layout(location=2) in vec4 tint;
layout(location=3) in vec4 animation;
uniform vec2 viewport;
out vec2 spriteUv;
out vec4 spriteTint;
void main() {
    vec2 corners[6] = vec2[6](vec2(0,0), vec2(1,0), vec2(0,1), vec2(0,1), vec2(1,0), vec2(1,1));
    vec2 corner = corners[gl_VertexID];
    vec2 local = corner - vec2(0.5);
    vec2 world = vec2(dot(row0.xy, local) + row0.w, dot(row1.xy, local) + row1.w);
    gl_Position = vec4(world.x / viewport.x * 2.0 - 1.0, 1.0 - world.y / viewport.y * 2.0, 0, 1);
    spriteUv = vec2((corner.x + animation.x) * animation.y, corner.y);
    spriteTint = tint;
}`;
const fragmentSource = `#version 300 es
precision highp float;
uniform sampler2D spriteTexture;
in vec2 spriteUv;
in vec4 spriteTint;
out vec4 outputColor;
void main() {
    ivec2 dimensions = textureSize(spriteTexture, 0);
    ivec2 texel = clamp(ivec2(floor(spriteUv * vec2(dimensions) + vec2(${TEXEL_BIAS}))), ivec2(0), dimensions - ivec2(1));
    outputColor = texelFetch(spriteTexture, texel, 0) * spriteTint;
}`;

export async function createWebGPU(canvas, bitmap, onLost) {
    if (!isSecureContext || !navigator.gpu) throw new Error('WebGPU API unavailable in this context');
    const adapter = await navigator.gpu.requestAdapter({ powerPreference: 'high-performance' });
    if (!adapter) throw new Error('WebGPU adapter unavailable');
    const device = await adapter.requestDevice();
    let disposed = false;
    const errors = [];
    device.addEventListener('uncapturederror', event => { event.preventDefault(); errors.push(String(event.error.message)); });
    try {
        const context = canvas.getContext('webgpu');
        if (!context) throw new Error('WebGPU canvas unavailable');
        const format = navigator.gpu.getPreferredCanvasFormat();
        context.configure({ device, format, alphaMode: 'opaque', usage: GPUTextureUsage.RENDER_ATTACHMENT | GPUTextureUsage.COPY_SRC });
        const module = device.createShaderModule({ code: wgsl });
        const diagnostics = await module.getCompilationInfo();
        const failures = diagnostics.messages.filter(message => message.type === 'error');
        if (failures.length) throw new Error(failures.map(message => message.message).join('; '));
        const pipeline = await device.createRenderPipelineAsync({
            layout: 'auto',
            vertex: { module, entryPoint: 'vertex_main', buffers: [{ arrayStride: STRIDE * 4, stepMode: 'instance', attributes: [0, 1, 2, 3].map(location => ({ shaderLocation: location, offset: location * 16, format: 'float32x4' })) }] },
            fragment: { module, entryPoint: 'fragment_main', targets: [{ format, blend: { color: { srcFactor: 'src-alpha', dstFactor: 'one-minus-src-alpha' }, alpha: { srcFactor: 'one', dstFactor: 'one-minus-src-alpha' } } }] },
            primitive: { topology: 'triangle-list', cullMode: 'none' }
        });
        const instances = device.createBuffer({ size: COUNT * STRIDE * 4, usage: GPUBufferUsage.VERTEX | GPUBufferUsage.COPY_DST });
        const uniform = device.createBuffer({ size: 16, usage: GPUBufferUsage.UNIFORM | GPUBufferUsage.COPY_DST });
        device.queue.writeBuffer(uniform, 0, new Float32Array([WIDTH, HEIGHT, 0, 0]));
        const texture = device.createTexture({ size: [bitmap.width, bitmap.height], format: 'rgba8unorm', usage: GPUTextureUsage.TEXTURE_BINDING | GPUTextureUsage.COPY_DST | GPUTextureUsage.RENDER_ATTACHMENT });
        device.queue.copyExternalImageToTexture({ source: bitmap }, { texture, premultipliedAlpha: false }, [bitmap.width, bitmap.height]);
        const bindGroup = device.createBindGroup({ layout: pipeline.getBindGroupLayout(0), entries: [{ binding: 0, resource: { buffer: uniform } }, { binding: 1, resource: texture.createView() }] });
        const info = adapter.info;
        device.lost.then(details => { if (!disposed) onLost('WebGPU device lost: ' + details.reason + ' ' + details.message); });
        let lastTexture;
        return {
            kind: 'webgpu', canvas, errors,
            info: { vendor: info?.vendor ?? '', architecture: info?.architecture ?? '', description: info?.description ?? '', isFallbackAdapter: info?.isFallbackAdapter ?? adapter.isFallbackAdapter ?? null, timestampQueryAvailable: adapter.features.has('timestamp-query'), timestampQueryMeasured: false },
            render(data) {
                device.queue.writeBuffer(instances, 0, data);
                const encoder = device.createCommandEncoder();
                lastTexture = context.getCurrentTexture();
                const pass = encoder.beginRenderPass({ colorAttachments: [{ view: lastTexture.createView(), clearValue: CLEAR, loadOp: 'clear', storeOp: 'store' }] });
                pass.setPipeline(pipeline);
                pass.setBindGroup(0, bindGroup);
                pass.setVertexBuffer(0, instances);
                pass.draw(6, data.length / STRIDE);
                pass.end();
                device.queue.submit([encoder.finish()]);
            },
            async readPixels() {
                const bytesPerRow = Math.ceil(WIDTH * 4 / 256) * 256;
                const readback = device.createBuffer({ size: bytesPerRow * HEIGHT, usage: GPUBufferUsage.COPY_DST | GPUBufferUsage.MAP_READ });
                try {
                    const encoder = device.createCommandEncoder();
                    encoder.copyTextureToBuffer({ texture: lastTexture }, { buffer: readback, bytesPerRow }, [WIDTH, HEIGHT]);
                    device.queue.submit([encoder.finish()]);
                    await readback.mapAsync(GPUMapMode.READ);
                    const source = new Uint8Array(readback.getMappedRange());
                    const pixels = new Uint8Array(WIDTH * HEIGHT * 4);
                    for (let row = 0; row < HEIGHT; row++) pixels.set(source.subarray(row * bytesPerRow, row * bytesPerRow + WIDTH * 4), row * WIDTH * 4);
                    if (format.startsWith('bgra')) for (let offset = 0; offset < pixels.length; offset += 4) { const red = pixels[offset]; pixels[offset] = pixels[offset + 2]; pixels[offset + 2] = red; }
                    return pixels;
                } finally { readback.destroy(); }
            },
            waitIdle: () => device.queue.onSubmittedWorkDone(),
            forceLoss: () => device.destroy(),
            dispose() { disposed = true; context.unconfigure(); instances.destroy(); uniform.destroy(); texture.destroy(); device.destroy(); }
        };
    } catch (error) { disposed = true; device.destroy(); throw error; }
}

export async function createWebGL(canvas, bitmap, onLost) {
    const context = canvas.getContext('webgl2', { alpha: false, antialias: false, premultipliedAlpha: false });
    if (!context) throw new Error('WebGL 2 unavailable');
    let disposed = false;
    const errors = [];
    canvas.addEventListener('webglcontextlost', event => { event.preventDefault(); if (!disposed) onLost('WebGL 2 context lost'); });
    const shaders = [];
    try {
        for (const [type, source] of [[context.VERTEX_SHADER, vertexSource], [context.FRAGMENT_SHADER, fragmentSource]]) {
            const shader = context.createShader(type);
            shaders.push(shader);
            context.shaderSource(shader, source);
            context.compileShader(shader);
            if (!context.getShaderParameter(shader, context.COMPILE_STATUS)) throw new Error(context.getShaderInfoLog(shader));
        }
        const program = context.createProgram();
        for (const shader of shaders) context.attachShader(program, shader);
        context.linkProgram(program);
        if (!context.getProgramParameter(program, context.LINK_STATUS)) throw new Error(context.getProgramInfoLog(program));
        for (const shader of shaders) context.deleteShader(shader);
        const vertexArray = context.createVertexArray();
        context.bindVertexArray(vertexArray);
        const instances = context.createBuffer();
        context.bindBuffer(context.ARRAY_BUFFER, instances);
        context.bufferData(context.ARRAY_BUFFER, COUNT * STRIDE * 4, context.STREAM_DRAW);
        for (let location = 0; location < 4; location++) {
            context.enableVertexAttribArray(location);
            context.vertexAttribPointer(location, 4, context.FLOAT, false, STRIDE * 4, location * 16);
            context.vertexAttribDivisor(location, 1);
        }
        const texture = context.createTexture();
        context.activeTexture(context.TEXTURE0);
        context.bindTexture(context.TEXTURE_2D, texture);
        context.pixelStorei(context.UNPACK_COLORSPACE_CONVERSION_WEBGL, context.NONE);
        context.texImage2D(context.TEXTURE_2D, 0, context.RGBA8, context.RGBA, context.UNSIGNED_BYTE, bitmap);
        for (const parameter of [context.TEXTURE_MIN_FILTER, context.TEXTURE_MAG_FILTER]) context.texParameteri(context.TEXTURE_2D, parameter, context.NEAREST);
        for (const parameter of [context.TEXTURE_WRAP_S, context.TEXTURE_WRAP_T]) context.texParameteri(context.TEXTURE_2D, parameter, context.CLAMP_TO_EDGE);
        context.useProgram(program);
        context.uniform2f(context.getUniformLocation(program, 'viewport'), WIDTH, HEIGHT);
        context.uniform1i(context.getUniformLocation(program, 'spriteTexture'), 0);
        context.enable(context.BLEND);
        context.blendFuncSeparate(context.SRC_ALPHA, context.ONE_MINUS_SRC_ALPHA, context.ONE, context.ONE_MINUS_SRC_ALPHA);
        context.clearColor(...CLEAR);
        context.viewport(0, 0, WIDTH, HEIGHT);
        const debug = context.getExtension('WEBGL_debug_renderer_info');
        return {
            kind: 'webgl2', canvas, errors,
            info: { vendor: debug ? context.getParameter(debug.UNMASKED_VENDOR_WEBGL) : context.getParameter(context.VENDOR), renderer: debug ? context.getParameter(debug.UNMASKED_RENDERER_WEBGL) : context.getParameter(context.RENDERER), gpuTimeMeasured: false },
            render(data) {
                context.bindBuffer(context.ARRAY_BUFFER, instances);
                context.bufferSubData(context.ARRAY_BUFFER, 0, data);
                context.bindVertexArray(vertexArray);
                context.clear(context.COLOR_BUFFER_BIT);
                context.drawArraysInstanced(context.TRIANGLES, 0, 6, data.length / STRIDE);
            },
            async readPixels() {
                const source = new Uint8Array(WIDTH * HEIGHT * 4);
                context.readPixels(0, 0, WIDTH, HEIGHT, context.RGBA, context.UNSIGNED_BYTE, source);
                const pixels = new Uint8Array(source.length);
                for (let row = 0; row < HEIGHT; row++) pixels.set(source.subarray((HEIGHT - row - 1) * WIDTH * 4, (HEIGHT - row) * WIDTH * 4), row * WIDTH * 4);
                return pixels;
            },
            waitIdle: async () => { context.finish(); const error = context.getError(); if (error !== context.NO_ERROR) errors.push('WebGL error ' + error); },
            forceLoss: () => context.getExtension('WEBGL_lose_context')?.loseContext(),
            dispose() { disposed = true; context.deleteBuffer(instances); context.deleteTexture(texture); context.deleteProgram(program); context.deleteVertexArray(vertexArray); context.getExtension('WEBGL_lose_context')?.loseContext(); }
        };
    } catch (error) { disposed = true; context.getExtension('WEBGL_lose_context')?.loseContext(); throw error; }
}
