require('dotenv').config();

const { spawn } = require('child_process');
const crypto    = require('crypto');
const { getPool, sql } = require('../backend/db');

const runId       = process.env.TESTVAULT_RUN_ID || crypto.randomUUID();
const triggeredBy = process.env.TRIGGERED_BY     || 'scheduler';
const startTime   = new Date();

console.log('╔══════════════════════════════════════╗');
console.log('║         TestVault Runner             ║');
console.log('╚══════════════════════════════════════╝');
console.log(`Run ID:     ${runId}`);
console.log(`Triggered:  ${triggeredBy}`);
console.log(`Started:    ${startTime.toISOString()}`);
console.log('──────────────────────────────────────\n');

async function startRun() {
  const db = await getPool();

  await db.request()
    .input('id',          sql.VarChar, runId)
    .input('triggeredBy', sql.VarChar, triggeredBy)
    .input('environment', sql.VarChar, process.env.NODE_ENV || 'test')
    .input('browser',     sql.VarChar, 'chromium')
    .query(`
      IF NOT EXISTS (SELECT 1 FROM test_runs WHERE id = @id)
        INSERT INTO test_runs
          (id, triggered_by, environment, browser, status, total, passed, failed, skipped)
        VALUES
          (@id, @triggeredBy, @environment, @browser, 'running', 0, 0, 0, 0)
    `);

  console.log('[TestVault] Run registered in DB ✓\n');
}

async function runTests() {
  await startRun();

  return new Promise((resolve, reject) => {
    const child = spawn(
      'npx',
      ['playwright', 'test'],
      {
        stdio: 'inherit',
        shell: true,
        env: {
          ...process.env,
          TESTVAULT_RUN_ID: runId,
          TRIGGERED_BY:     triggeredBy,
        },
      }
    );

    child.on('exit',  (code) => resolve(code ?? 0));
    child.on('error', (err)  => reject(err));
  });
}

(async () => {
  let exitCode = 0;

  try {
    exitCode = await runTests();
  } catch (err) {
    console.error('[TestVault] Runner Error:', err.message);
    exitCode = 1;
  }

  const endTime     = new Date();
  const durationSec = ((endTime - startTime) / 1000).toFixed(1);

  console.log('\n──────────────────────────────────────');
  console.log(`Finished:   ${endTime.toISOString()}`);
  console.log(`Duration:   ${durationSec}s`);
  console.log(`Exit Code:  ${exitCode}`);
  console.log('╚══════════════════════════════════════╝\n');

  process.exit(exitCode);
})();