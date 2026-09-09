const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawn, execFileSync } = require('node:child_process');

const root = path.resolve(__dirname, '../..');
const option = (name, fallback) => process.argv.find(argument => argument.startsWith('--' + name + '='))?.slice(name.length + 3) ?? fallback;
const output = path.resolve(root, option('output', path.join('artifacts/webgpu-validation', new Date().toISOString().replace(/[:.]/g, '-'))));
const only = new RegExp(option('only', '.*'));
const report = { started: new Date().toISOString(), output, sourceCommit: execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), physicalMobileTested: false, productionReady: false, gates: [], notCovered: ['Physical Android/iOS phone and tablet performance', 'Full-match WebGPU device-loss recovery on physical devices'] };
if (fs.existsSync(output) && fs.readdirSync(output).length) throw new Error('Refusing to replace an existing verification directory: ' + output);
fs.mkdirSync(output, { recursive: true });
report.selection = option('only', '.*');
report.fullMatrixRequested = report.selection === '.*';
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const protectedFiles = ['artifacts/web-compatible-latest.json', 'project.godot', 'export_presets.cfg', 'TouhouWuxiaSurvivor.csproj', 'CHANGELOG.md', 'release/TouhouWuxiaSurvivor_alpha-0.1.7.exe', 'release/RECEIPT_alpha-0.1.7.json'];
report.protectedBefore = Object.fromEntries(protectedFiles.map(file => [file, hash(path.join(root, file))]));

function save() {
    fs.writeFileSync(path.join(output, 'report.json'), JSON.stringify(report, null, 2), 'utf8');
    fs.writeFileSync(path.join(root, 'artifacts/webgpu-validation/latest.json'), JSON.stringify({ output, report: path.join(output, 'report.json') }, null, 2), 'utf8');
}

async function run(name, executable, arguments_, environment = {}, timeout = 600000) {
    if (!only.test(name)) return;
    const log = path.join(output, name + '.log');
    const descriptor = fs.openSync(log, 'wx');
    const start = Date.now();
    console.log('GATE_START', name, log);
    const result = await new Promise(resolve => {
        const child = spawn(executable, arguments_, { cwd: root, env: { ...process.env, ...environment }, windowsHide: true, stdio: ['ignore', descriptor, descriptor] });
        let timedOut = false;
        let completed = false;
        const timer = setTimeout(() => {
            timedOut = true;
            if (child.pid) {
                if (process.platform === 'win32') spawn('taskkill.exe', ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
                else child.kill('SIGTERM');
            }
        }, timeout);
        const finish = result => {
            if (completed) return;
            completed = true;
            clearTimeout(timer);
            resolve({ ...result, timedOut });
        };
        child.on('error', error => finish({ exitCode: null, error: String(error) }));
        child.on('close', (exitCode, signal) => finish({ exitCode, signal }));
    });
    fs.closeSync(descriptor);
    report.gates.push({ name, log, seconds: (Date.now() - start) / 1000, ...result, passed: result.exitCode === 0 && !result.timedOut });
    save();
    console.log('GATE_END', name, JSON.stringify(report.gates.at(-1)));
}

const node = (name, script, arguments_ = [], environment = {}, timeout) => run(name, process.execPath, [path.join(root, script), ...arguments_], environment, timeout);

async function main() {
    save();
    const testFiles = ['tools/platform/verification_paths.test.cjs', 'tools/platform/verify_deployment.test.cjs', 'tools/platform/loader.test.cjs', 'tools/platform/batch_color_state.test.cjs', 'tools/webgpu/scene.test.mjs', 'tools/webgpu/server.test.cjs', 'tools/webgpu/browser_evidence.test.cjs'];
    await run('unit-node', process.execPath, ['--test', ...testFiles]);
    await run('source-build', 'dotnet.exe', ['build', 'TouhouWuxiaSurvivor.csproj', '--configuration', 'Debug']);
    await run('core-balance', 'dotnet.exe', ['run', '--project', 'tests/rebirth/Rebirth.Tests.csproj', '--configuration', 'Release', '--', '--balance']);
    await run('localization', 'dotnet.exe', ['run', '--project', 'tests/localization/Localization.Tests.csproj', '--configuration', 'Release']);
    await run('windows-standalone', process.env.POWERSHELL7_EXE || 'pwsh.exe', ['-NoProfile', '-File', path.join(root, 'tools/rebirth/verify_release.ps1'), '-OutputDirectory', path.join(output, 'windows'), '-ArtifactSourceCommit', '82563e6535f42e35cfe67318d16a116b7b04dee7']);
    await node('actual-visual-comparison', 'tools/webgpu/visual_comparison.cjs', ['--output=' + path.join(output, 'visual-comparison')]);
    const visualReportPath = path.join(output, 'visual-comparison/report.json');
    report.renderedWorkEquivalent = fs.existsSync(visualReportPath) && JSON.parse(fs.readFileSync(visualReportPath, 'utf8')).passed === true;
    report.webgpuTimingScope = report.renderedWorkEquivalent ? 'Hardware timing with matching static visual fixtures; physical mobile acceptance still outstanding.' : 'Diagnostic timings only: equivalent rendered work has NOT passed. Numeric FPS gates are not rendering or performance acceptance.';
    for (const backend of ['webgl2', 'webgpu']) {
        const pointer = path.join(root, backend === 'webgpu' ? 'artifacts/web-webgpu-latest.json' : 'artifacts/web-compatible-latest.json');
        const backendOutput = path.join(output, backend);
        fs.mkdirSync(backendOutput, { recursive: true });
        const environment = { TOUHOU_VERIFY_BUILD: pointer, TOUHOU_VERIFY_OUTPUT: backendOutput };
        for (const hero of ['reimu', 'marisa']) {
            await node(backend + '-probe-' + hero, 'tools/webgpu/game_probe.cjs', ['--build=' + pointer, '--expected=' + backend, '--fixture=combat-performance', '--hero=' + hero, '--output=' + path.join(backendOutput, 'probe-' + hero)]);
        }
        await node(backend + '-batches', 'tools/platform/verify_batches.cjs', [], environment);
        await node(backend + '-color-stress', 'tools/platform/verify_batches.cjs', ['--color-state-stress'], environment);
        for (const deployment of [false, true]) {
            for (const isolation of [true, false]) {
                await node(backend + '-web-' + (deployment ? 'deployment' : 'raw') + '-' + (isolation ? 'isolated' : 'unisolated'), 'tools/platform/verify_web.cjs', ['--compatible', ...(deployment ? ['--deployment'] : []), ...(isolation ? [] : ['--unisolated'])], environment);
            }
        }
        for (const hero of ['reimu', 'marisa']) {
            for (const language of ['zh', 'en']) {
                await node(backend + '-combat-' + hero + '-' + language, 'tools/platform/benchmark_combat.cjs', ['--enforce', '--build=' + pointer, '--expected=' + backend, '--hero=' + hero, '--language=' + language, '--output=' + path.join(backendOutput, 'combat-' + hero + '-' + language + '.json')], environment);
            }
            await node(backend + '-sustained-' + hero, 'tools/platform/benchmark_combat.cjs', ['--enforce', '--build=' + pointer, '--expected=' + backend, '--hero=' + hero, '--loads=320', '--seconds=60', '--output=' + path.join(backendOutput, 'sustained-' + hero + '.json')], environment);
        }
        await node(backend + '-language', 'tools/platform/verify_language.cjs', [], environment);
        await node(backend + '-portraits', 'tools/platform/verify_hero_portraits.cjs', [], environment);
        await node(backend + '-loading', 'tools/platform/verify_loading.cjs', ['--compatible'], environment);
        await node(backend + '-raw-loading', 'tools/platform/verify_raw_loading.cjs', [], environment);
        await node(backend + '-dpr', 'tools/platform/verify_performance.cjs', ['--compatible'], environment);
    }
    for (const fault of ['missing-api', 'no-adapter', 'device-rejected', 'device-loss']) {
        await node('webgpu-fault-' + fault, 'tools/webgpu/game_probe.cjs', ['--fault=' + fault, '--fixture=combat-performance', '--output=' + path.join(output, 'fault-' + fault)], {}, 180000);
    }
    report.protectedAfter = Object.fromEntries(protectedFiles.map(file => [file, hash(path.join(root, file))]));
    report.protectedUnchanged = JSON.stringify(report.protectedBefore) === JSON.stringify(report.protectedAfter);
    report.finished = new Date().toISOString();
    report.passed = report.gates.length > 0 && report.gates.every(gate => gate.passed) && report.protectedUnchanged;
    save();
    console.log('FULL_VALIDATION_RESULT', JSON.stringify({ output, passed: report.passed, productionReady: report.productionReady, failed: report.gates.filter(gate => !gate.passed).map(gate => gate.name), protectedUnchanged: report.protectedUnchanged }));
    if (!report.passed) process.exitCode = 1;
}

main().catch(error => { report.runnerFailure = String(error.stack || error); save(); console.error(error); process.exitCode = 1; });
