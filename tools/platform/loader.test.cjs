const test = require('node:test');
const assert = require('node:assert/strict');
const { DownloadProgress, bytes, duration } = require('../../platform/web/loader.js');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

function fixture() {
    let time = 0;
    const progress = new DownloadProgress([{ bytes: 1000000 }, { bytes: 3000000 }], () => time);
    return { progress, advance: milliseconds => { time += milliseconds; } };
}

test('total and per-file received bytes are exact, speed and ETA use received data', () => {
    const { progress, advance } = fixture();
    assert.equal(progress.snapshot().total, 4000000);
    assert.equal(progress.snapshot().speed, null);
    assert.equal(progress.snapshot().eta, null);
    advance(1000);
    progress.receive(progress.files[0], 500000);
    const state = progress.snapshot();
    assert.equal(state.loaded, 500000);
    assert.equal(state.percent, 12.5);
    assert.equal(state.speed, 500000);
    assert.equal(state.eta, 7);
    assert.equal(bytes(state.speed), '500.0 KB');
    assert.equal(bytes(state.total), '4.00 MB');
});

test('stall stops speed and ETA without inventing progress; receiving resumes', () => {
    const { progress, advance } = fixture();
    advance(1000);
    progress.receive(progress.files[0], 500000);
    progress.snapshot();
    for (let tick = 0; tick < 32; tick++) { advance(250); progress.snapshot(); }
    const state = progress.snapshot();
    assert.equal(state.stalled, true);
    assert.equal(state.speed, 0);
    assert.equal(state.loaded, 500000);
    assert.equal(state.eta, null);
    advance(250);
    progress.receive(progress.files[0], 250000);
    assert.equal(progress.snapshot().stalled, false);
    assert.ok(progress.snapshot().speed > 0);
});

test('100 percent requires all streams to finish, cache and initialization tracked separately', () => {
    const { progress, advance } = fixture();
    progress.receive(progress.files[0], 1000000);
    progress.complete(progress.files[0], true);
    progress.receive(progress.files[1], 2999999);
    assert.equal(progress.snapshot().percent, 99.9);
    progress.receive(progress.files[1], 1);
    assert.equal(progress.snapshot().finished, false);
    assert.equal(progress.snapshot().percent, 99.9);
    progress.complete(progress.files[1], true);
    advance(2000);
    const state = progress.snapshot();
    assert.equal(state.finished, true);
    assert.equal(state.percent, 100);
    assert.equal(state.eta, null);
    assert.equal(state.seconds, 2);
    assert.equal(state.cached, 2);
});

test('invalid or incomplete transfer sizes fail instead of misleading the user', () => {
    const { progress } = fixture();
    for (const length of [-1, 0.5, NaN, Infinity, 1000001]) {
        assert.throws(() => progress.receive(progress.files[0], length));
    }
    assert.throws(() => progress.complete(progress.files[0]));
    assert.equal(progress.snapshot().loaded, 0);
});

test('unknown ETA and byte units never display Infinity or NaN', () => {
    assert.equal(duration(Infinity), '正在估算');
    assert.equal(duration(NaN), '正在估算');
    assert.equal(duration(61), '约 1 分 1 秒');
    assert.equal(bytes(0), '0 B');
    assert.equal(bytes(12345678), '12.35 MB');
});

for (const threads of [undefined, true, false]) {
    test(`loader passes the actual thread requirement (${threads}) and drains raw downloads`, async () => {
        const elements = new Map();
        const document = {
            baseURI: 'https://example.test/TouhouSurvivor/', body: { dataset: {} },
            getElementById(name) {
                if (!elements.has(name)) elements.set(name, { dataset: {}, addEventListener() {}, setAttribute() {}, remove() { this.removed = true; } });
                return elements.get(name);
            }
        };
        let requirements;
        let drained = false;
        let requests = 0;
        let context;
        const originalFetch = async () => { requests++; return new Response(new ReadableStream({
            start(controller) { controller.enqueue(new Uint8Array([1, 2])); controller.enqueue(new Uint8Array([3, 4])); controller.close(); },
            cancel() { assert.fail('Download must reach EOF rather than being cancelled'); }
        })); };
        class Engine {
            static getMissingFeatures(options) { requirements = options.threads; return []; }
            async startGame() {
                const response = await context.fetch('index.pck');
                assert.deepEqual([...new Uint8Array(await response.arrayBuffer())], [1, 2, 3, 4]);
                drained = true;
            }
        }
        context = vm.createContext({ document, Engine, fetch: originalFetch, URL, Response, ReadableStream, AbortController, performance: { now: () => 0, getEntriesByName: () => [] }, setInterval, clearInterval });
        vm.runInContext(fs.readFileSync(path.join(__dirname, '../../platform/web/loader.js'), 'utf8'), context);
        const loader = context.TouhouLoading.create({ fileSizes: { 'index.pck': 4 } }, null, threads);
        await loader.start();
        assert.equal(requirements, threads ?? true);
        assert.equal(drained, true);
        assert.equal(requests, 1);
        assert.equal(document.body.dataset.gameReady, 'true');
        assert.equal(elements.get('loading').dataset.loaded, '4');
        assert.equal(elements.get('loading').removed, true);
        assert.equal(context.fetch, originalFetch);
    });
}
