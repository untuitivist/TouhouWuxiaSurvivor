const path = require('node:path');
const { verify } = require('../platform/verify_web.cjs');

async function main() {
    await require('../platform/verify_raw_loading.cjs').verifyRawLoading();
    for (const isolation of [false, true]) {
        for (const deployment of [false, true]) {
            await verify(require('playwright'), path.resolve(__dirname, '../..'), { compatible: true, isolation, deployment });
        }
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
