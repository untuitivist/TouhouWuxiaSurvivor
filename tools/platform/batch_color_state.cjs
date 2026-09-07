function installBatchColorStress() {
    const stats = { draws: 0, missingColor: 0, explicitColor: 0 };
    globalThis.batchColorStress = stats;
    const prototypes = [globalThis.WebGL2RenderingContext?.prototype].filter(Boolean);
    const colors = [[0, 0, 0, 1], [1, 0.125, 0.5, 1], [0, 0, 0, 0]];
    for (const prototype of prototypes) {
        const programs = new WeakMap();
        for (const method of ['drawElementsInstanced', 'drawArraysInstanced']) {
            const draw = prototype[method];
            prototype[method] = function (...args) {
                const program = this.getParameter(this.CURRENT_PROGRAM);
                if (!program) return draw.apply(this, args);
                if (!programs.has(program)) programs.set(program,
                    this.getAttribLocation(program, 'color_attrib') === 3 && this.getAttribLocation(program, 'instance_xform0') === 1);
                if (!programs.get(program)) return draw.apply(this, args);
                const saved = this.getVertexAttrib(3, this.CURRENT_VERTEX_ATTRIB);
                const enabled = this.getVertexAttrib(3, this.VERTEX_ATTRIB_ARRAY_ENABLED);
                const color = colors[stats.draws++ % colors.length];
                stats[enabled ? 'explicitColor' : 'missingColor']++;
                this.vertexAttrib4f(3, ...color);
                try { return draw.apply(this, args); }
                finally { this.vertexAttrib4f(3, ...saved); }
            };
        }
    }
}

module.exports = { installBatchColorStress };
