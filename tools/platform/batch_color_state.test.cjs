const test = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const { installBatchColorStress } = require('./batch_color_state.cjs');

function fixture({ explicit = false, instancedShader = true, fail = false } = {}) {
    class Context {
        CURRENT_PROGRAM = 1;
        CURRENT_VERTEX_ATTRIB = 2;
        VERTEX_ATTRIB_ARRAY_ENABLED = 3;
        program = {};
        color = [0.2, 0.3, 0.4, 0.5];
        samples = [];
        getParameter() { return this.program; }
        getAttribLocation(program, name) { return instancedShader ? name === 'color_attrib' ? 3 : 1 : -1; }
        getVertexAttrib(index, kind) { return kind === this.CURRENT_VERTEX_ATTRIB ? [...this.color] : explicit; }
        vertexAttrib4f(index, ...color) { this.color = color; }
        drawElementsInstanced(...args) {
            this.samples.push({ color: [...this.color], args });
            if (fail) throw new Error('draw failure');
            return 42;
        }
        drawArraysInstanced(...args) {
            this.samples.push({ color: [...this.color], args });
            return 43;
        }
    }
    const scope = vm.createContext({ WebGL2RenderingContext: Context });
    vm.runInContext('(' + installBatchColorStress.toString() + ')()', scope);
    return { context: new Context(), stats: scope.batchColorStress };
}

test('default-color stress alternates black, tint and transparent across both draw APIs', () => {
    const { context, stats } = fixture();
    assert.equal(context.drawElementsInstanced(4, 6, 2, 0, 3), 42);
    assert.equal(context.drawArraysInstanced(4, 0, 4, 3), 43);
    context.drawElementsInstanced(4, 6, 2, 0, 3);
    assert.deepEqual(context.samples.map(sample => sample.color), [[0, 0, 0, 1], [1, 0.125, 0.5, 1], [0, 0, 0, 0]]);
    assert.deepEqual(context.samples[0].args, [4, 6, 2, 0, 3]);
    assert.deepEqual(context.color, [0.2, 0.3, 0.4, 0.5]);
    assert.equal(stats.draws, 3);
    assert.equal(stats.missingColor, 3);
});

test('explicit-color meshes are exercised without disabling their color arrays', () => {
    const { context, stats } = fixture({ explicit: true });
    context.drawElementsInstanced();
    assert.equal(context.getVertexAttrib(3, context.VERTEX_ATTRIB_ARRAY_ENABLED), true);
    assert.equal(stats.explicitColor, 1);
    assert.equal(stats.missingColor, 0);
    assert.deepEqual(context.color, [0.2, 0.3, 0.4, 0.5]);
});

test('unrelated shaders and absent programs are untouched', () => {
    const { context, stats } = fixture({ instancedShader: false });
    context.drawElementsInstanced();
    context.program = null;
    context.drawArraysInstanced();
    assert.equal(stats.draws, 0);
    assert.deepEqual(context.samples.map(sample => sample.color), [[0.2, 0.3, 0.4, 0.5], [0.2, 0.3, 0.4, 0.5]]);
});

test('failed draws restore the original default color', () => {
    const { context } = fixture({ fail: true });
    assert.throws(() => context.drawElementsInstanced(), /draw failure/);
    assert.deepEqual(context.color, [0.2, 0.3, 0.4, 0.5]);
});
