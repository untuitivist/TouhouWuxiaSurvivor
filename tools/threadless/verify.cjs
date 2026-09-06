const path = require('node:path');
const { verify } = require('../platform/verify_web.cjs');

async function main() {
    for (const isolation of [false, true]) {
        await verify(require('playwright'), path.resolve(__dirname, '../..'), { compatible: true, isolation });
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
