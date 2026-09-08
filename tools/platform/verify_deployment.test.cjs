const assert = require('node:assert/strict');
const { test } = require('node:test');
const { classifyBrowserConsole, startupTimeout } = require('./verify_deployment.cjs');

test('public startup budget is explicit, bounded and cannot disable timeouts', () => {
    assert.equal(startupTimeout('180000'), 180000);
    assert.equal(startupTimeout('600000'), 600000);
    for (const invalid of ['', '0', '-1', '179999', '900001', 'Infinity', 'NaN', '600000.5'])
        assert.throws(() => startupTimeout(invalid));
});

const waiting = ['still waiting on run dependencies:', 'dependency: wasm-instantiate', '(end of list)'];
const events = (texts, startup = true, type = 'error') => texts.map(text => ({ text, startup, type }));

test('complete known startup waiting blocks are retained as diagnostics', () => {
    const result = classifyBrowserConsole(events([...waiting, ...waiting]));
    assert.deepEqual(result.errors, []);
    assert.equal(result.startupDiagnostics.length, 2);
    assert.deepEqual(result.startupDiagnostics[0], events(waiting));
});

test('the same messages after startup remain errors', () => {
    assert.deepEqual(classifyBrowserConsole(events(waiting, false)).errors, waiting);
});

test('unknown dependencies and incomplete blocks remain errors', () => {
    const unknown = [waiting[0], 'dependency: unknown', waiting[2]];
    assert.deepEqual(classifyBrowserConsole(events(unknown)).errors, unknown);
    assert.deepEqual(classifyBrowserConsole(events(waiting.slice(0, 2))).errors, waiting.slice(0, 2));
});

test('runtime failures mixed with waiting are never suppressed', () => {
    const failure = 'Aborted(WebAssembly instantiation failed)';
    assert.deepEqual(classifyBrowserConsole(events([...waiting, failure])).errors, [failure]);
    const interrupted = [waiting[0], failure, ...waiting.slice(1)];
    assert.deepEqual(classifyBrowserConsole(events(interrupted)).errors, interrupted);
});

test('graphics warnings remain failures and ordinary output does not', () => {
    const warning = 'WebGL: INVALID_OPERATION';
    assert.deepEqual(classifyBrowserConsole(events([warning], false, 'warning')).errors, [warning]);
    assert.deepEqual(classifyBrowserConsole(events(['Godot Engine'], false, 'log')).errors, []);
});

test('blocks spanning successful startup are not treated as waiting', () => {
    const mixed = [...events(waiting.slice(0, 2)), ...events(waiting.slice(2), false)];
    assert.deepEqual(classifyBrowserConsole(mixed).errors, waiting);
});
