(function (scope) {
    'use strict';

    function bytes(value) {
        if (value >= 1000000) return `${(value / 1000000).toFixed(2)} MB`;
        if (value >= 1000) return `${(value / 1000).toFixed(1)} KB`;
        return `${Math.floor(value)} B`;
    }

    function duration(seconds) {
        if (!Number.isFinite(seconds)) return '正在估算';
        const rounded = Math.max(1, Math.ceil(seconds));
        if (rounded < 60) return `约 ${rounded} 秒`;
        return `约 ${Math.floor(rounded / 60)} 分 ${rounded % 60} 秒`;
    }

    class DownloadProgress {
        constructor(files, now = () => performance.now()) {
            this.now = now;
            this.files = files.map(file => ({ ...file, loaded: 0, done: false, cached: false }));
            this.total = this.files.reduce((sum, file) => sum + file.bytes, 0);
            this.started = now();
            this.lastData = this.started;
            this.samples = [{ time: this.started, bytes: 0 }];
            this.finishedAt = null;
        }

        receive(file, length) {
            if (!Number.isSafeInteger(length) || length < 0 || file.loaded + length > file.bytes) throw new Error('资源大小与清单不一致，请重新加载。');
            file.loaded += length;
            if (length > 0) this.lastData = this.now();
        }

        complete(file, cached = false) {
            if (file.loaded !== file.bytes) throw new Error('资源没有接收完整，请重新加载。');
            file.done = true;
            file.cached = cached;
            if (this.files.every(entry => entry.done)) this.finishedAt = this.now();
        }

        snapshot() {
            const time = this.now();
            const loaded = this.files.reduce((sum, file) => sum + file.loaded, 0);
            this.samples.push({ time, bytes: loaded });
            while (this.samples.length > 1 && this.samples[1].time <= time - 3000) this.samples.shift();
            const span = (time - this.samples[0].time) / 1000;
            const speed = span >= 0.5 ? Math.max(0, (loaded - this.samples[0].bytes) / span) : null;
            const finished = this.finishedAt !== null;
            const stalled = !finished && time - this.lastData >= 8000;
            return { loaded, total: this.total, percent: finished ? 100 : Math.min(99.9, this.total ? loaded / this.total * 100 : 0), speed: stalled ? 0 : speed, stalled, finished,
                eta: !finished && !stalled && speed > 0 ? (this.total - loaded) / speed : null,
                seconds: Math.floor((time - (this.finishedAt ?? this.started)) / 1000),
                cached: this.files.filter(file => file.cached).length,
                done: this.files.filter(file => file.done).length,
                count: this.files.length };
        }
    }

    function create(configuration, transfers, threads = true) {
        const compressed = Array.isArray(transfers) && typeof scope.DecompressionStream === 'function';
        const files = compressed ? transfers.map(file => ({ ...file })) : Object.entries(configuration.fileSizes)
            .filter(([file]) => /\.(wasm|pck)$/.test(file))
            .map(([url, size]) => ({ url, download: url, bytes: size, decodedBytes: size, compressed: false }));
        const monitor = new DownloadProgress(files);
        const elements = Object.fromEntries(['loading', 'notice', 'progress', 'amount', 'speed', 'remaining', 'percentage', 'phase', 'download-total', 'download-note', 'retry']
            .map(name => [name, document.getElementById(name)]));
        const originalFetch = scope.fetch;
        const abort = new AbortController();
        const responses = new Set();
        let failed = false;
        let timer;
        let wrappedFetch;
        let decodedMode = !compressed;

        elements['download-total'].textContent = `本次核心资源总量 ${bytes(monitor.total)}`;
        elements['download-note'].textContent = compressed
            ? '按压缩资源正文计量，不含少量页面、脚本及协议流量；命中缓存时，接收速度是缓存读取速度。'
            : '当前按资源读取量计量；若服务器使用 HTTP 压缩，此数值为解压后大小，不代表网络流量。';
        elements.retry.addEventListener('click', () => scope.location.reload());

        function restore() {
            if (scope.fetch === wrappedFetch) scope.fetch = originalFetch;
            responses.clear();
        }

        function render() {
            if (failed) return;
            const state = monitor.snapshot();
            elements.loading.dataset.loaded = String(state.loaded);
            elements.loading.dataset.total = String(state.total);
            elements.loading.dataset.speed = String(state.speed ?? 0);
            elements.loading.dataset.stage = state.finished ? 'initializing' : state.stalled ? 'waiting' : 'downloading';
            elements.progress.max = state.total || 1;
            elements.progress.value = state.loaded;
            elements.progress.setAttribute('aria-valuetext', `${bytes(state.loaded)}，共 ${bytes(state.total)}`);
            elements.amount.textContent = `${bytes(state.loaded)} / ${bytes(state.total)}`;
            elements.percentage.textContent = `${state.percent.toFixed(1)}%`;
            elements.speed.textContent = state.finished ? '接收完成' : state.speed === null ? '正在测量' : `${bytes(state.speed)}/s`;
            elements.remaining.textContent = state.finished ? '下载已完成' : state.stalled ? '等待数据' : duration(state.eta ?? Infinity);
            elements.phase.textContent = state.finished ? '02 / 初始化游戏' : `01 / 接收资源 ${state.done} / ${state.count}`;
            elements.notice.textContent = state.finished
                ? `资源已接收完毕，正在完成解压、编译引擎并初始化游戏。\n此阶段不再计算下载速度，已等待 ${state.seconds} 秒。`
                : state.stalled ? '暂未收到新数据，正在等待网络响应。\n已接收量不会假增长，请检查网络连接。'
                    : decodedMode ? '正在读取引擎和游戏资源包，请稍候。' : '正在下载引擎和游戏资源包，完成后将自动进入。';
            if (state.finished && state.cached === state.count) elements['download-note'].textContent = '核心资源已从浏览器缓存读取，本次无需重新下载这些资源。';
        }

        function fail(error) {
            if (failed) return;
            render();
            failed = true;
            clearInterval(timer);
            abort.abort();
            elements.loading.dataset.stage = 'error';
            elements.phase.textContent = '加载未完成';
            elements.notice.textContent = `加载中断，当前进度已停止。\n${error.message || String(error)}`;
            elements.speed.textContent = '已停止';
            elements.remaining.textContent = '请重试';
            elements.retry.hidden = false;
        }

        wrappedFetch = async function (input, options) {
            const url = new URL(typeof input === 'string' || input instanceof URL ? input : input.url, document.baseURI).href;
            const file = monitor.files.find(entry => new URL(entry.url, document.baseURI).href === url);
            if (!file) return originalFetch.call(scope, input, options);
            if (failed) throw new Error('下载已停止，请重试。');
            try {
                const download = new URL(file.download, document.baseURI).href;
                const response = await originalFetch.call(scope, download, { ...options, signal: abort.signal });
                if (!response.ok || !response.body) throw new Error(`资源请求失败：HTTP ${response.status}`);
                responses.add(response);
                if (file.compressed && response.headers.get('Content-Encoding')) throw new Error('压缩资源的服务器响应不符合加载要求。');
                if (!file.compressed && response.headers.get('Content-Encoding')) decodedMode = true;
                const sourceReader = response.body.getReader();
                const tracked = new ReadableStream({
                    async pull(controller) {
                        const result = await sourceReader.read();
                        if (!result.done) {
                            monitor.receive(file, result.value.byteLength);
                            controller.enqueue(result.value);
                            return;
                        }
                        const timings = performance.getEntriesByName(download);
                        const timing = timings[timings.length - 1];
                        monitor.complete(file, !!timing && timing.transferSize === 0 && timing.decodedBodySize > 0);
                        render();
                        sourceReader.releaseLock();
                        controller.close();
                    },
                    cancel(reason) { return sourceReader.cancel(reason); }
                });
                const decoded = file.compressed ? tracked.pipeThrough(new DecompressionStream('gzip')) : tracked;
                const reader = decoded.getReader();
                const body = new ReadableStream({
                    async pull(controller) {
                        try {
                            const result = await reader.read();
                            if (result.done) controller.close();
                            else controller.enqueue(result.value);
                        } catch (error) {
                            fail(error);
                            controller.error(error);
                        }
                    },
                    cancel(reason) { return reader.cancel(reason); }
                });
                return new Response(body, { headers: { 'Content-Type': /\.wasm$/.test(file.url) ? 'application/wasm' : 'application/octet-stream' } });
            } catch (error) {
                fail(error);
                throw error;
            }
        };

        render();
        timer = setInterval(render, 250);
        return { fail, async start() {
            if (failed) return;
            try {
                const missing = Engine.getMissingFeatures({ threads });
                if (missing.length) throw new Error('浏览器或服务器不满足运行条件：' + missing.join('、'));
                scope.fetch = wrappedFetch;
                const engine = new Engine(configuration);
                await engine.startGame();
                if (failed) return;
                clearInterval(timer);
                restore();
                document.body.dataset.gameReady = 'true';
                elements.loading.remove();
            } catch (error) { fail(error); }
        } };
    }

    const api = { DownloadProgress, bytes, duration, create };
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else scope.TouhouLoading = api;
})(globalThis);
