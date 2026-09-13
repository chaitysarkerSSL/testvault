// reporters/sql-reporter.js
const { getPool, sql } = require('../backend/db');

let runId   = null;
let startMs = null;

class SqlReporter {

  async onBegin(config, suite) {
    runId   = process.env.TESTVAULT_RUN_ID || require('crypto').randomUUID();
    startMs = Date.now();

    const total = suite.allTests().length;

    console.log(`\n[TestVault] Run ID: ${runId}`);
    console.log(`[TestVault] Total tests: ${total}\n`);

    const db = await getPool();

    await db.request()
      .input('id',          sql.VarChar, runId)
      .input('total',       sql.Int,     total)
      .input('triggeredBy', sql.VarChar, process.env.TRIGGERED_BY       || 'scheduler')
      .input('environment', sql.VarChar, process.env.NODE_ENV           || 'test')
      .input('browser',     sql.VarChar, process.env.PLAYWRIGHT_PROJECT || 'chromium')
      .query(`
        IF EXISTS (SELECT 1 FROM test_runs WHERE id = @id)
          UPDATE test_runs SET total = @total WHERE id = @id
        ELSE
          INSERT INTO test_runs (id, total, triggered_by, environment, browser, status)
          VALUES (@id, @total, @triggeredBy, @environment, @browser, 'running')
      `);
  }

  async onTestEnd(test, result) {
    const db = await getPool();

    const title    = test.titlePath().filter(Boolean).join(' > ');
    const status   = result.status;
    const errorMsg = result.error?.message ? result.error.message.slice(0, 2000) : null;
    const browser  = test.parent?.project()?.name || process.env.PLAYWRIGHT_PROJECT || 'chromium';

    let screenshot = null, video = null, trace = null;
    for (const a of result.attachments || []) {
      if (a.contentType?.includes('image') || a.name === 'screenshot') screenshot = a.path;
      if (a.contentType?.includes('video') || a.name === 'video')      video      = a.path;
      if (a.name === 'trace')                                           trace      = a.path;
    }

    await db.request()
      .input('runId',       sql.VarChar,  runId)
      .input('title',       sql.NVarChar, title)
      .input('status',      sql.VarChar,  status)
      .input('durationMs',  sql.Int,      result.duration    || 0)
      .input('browser',     sql.VarChar,  browser)
      .input('workerIndex', sql.Int,      result.workerIndex || 0)
      .input('retryCount',  sql.Int,      result.retry       || 0)
      .input('errorMsg',    sql.NVarChar, errorMsg)
      .input('screenshot',  sql.NVarChar, screenshot)
      .input('video',       sql.NVarChar, video)
      .input('trace',       sql.NVarChar, trace)
      .query(`
        INSERT INTO test_cases
          (run_id, title, status, duration_ms, browser, worker_index,
           retry_count, error_message, screenshot, video, trace)
        VALUES
          (@runId, @title, @status, @durationMs, @browser, @workerIndex,
           @retryCount, @errorMsg, @screenshot, @video, @trace)
      `);

    const icon = status === 'passed' ? '✓' : status === 'failed' ? '✗' : '○';
    console.log(`  [${icon}] ${title} (${result.duration}ms)`);
  }

  async onEnd() {
    const db         = await getPool();
    const durationMs = Date.now() - startMs;

    const stats = await db.request()
      .input('runId', sql.VarChar, runId)
      .query(`
        SELECT
          SUM(CASE WHEN status = 'passed'   THEN 1 ELSE 0 END) AS passed,
          SUM(CASE WHEN status = 'failed'   THEN 1 ELSE 0 END) AS failed,
          SUM(CASE WHEN status = 'skipped'  THEN 1 ELSE 0 END) AS skipped,
          SUM(CASE WHEN status = 'timedOut' THEN 1 ELSE 0 END) AS timedout
        FROM test_cases
        WHERE run_id = @runId
      `);

    const r          = stats.recordset[0];
    const passed     = r.passed   || 0;
    const failed     = (r.failed  || 0) + (r.timedout || 0);
    const skipped    = r.skipped  || 0;
    const finalStatus = failed > 0 ? 'failed' : 'passed';

    await db.request()
      .input('runId',      sql.VarChar, runId)
      .input('status',     sql.VarChar, finalStatus)
      .input('passed',     sql.Int,     passed)
      .input('failed',     sql.Int,     failed)
      .input('skipped',    sql.Int,     skipped)
      .input('durationMs', sql.BigInt,  durationMs)
      .query(`
        UPDATE test_runs SET
          finished_at = GETDATE(),
          status      = @status,
          passed      = @passed,
          failed      = @failed,
          skipped     = @skipped,
          duration_ms = @durationMs
        WHERE id = @runId
      `);

    console.log(`\n╔══════════════════════════════╗`);
    console.log(`║   TestVault Run Complete!    ║`);
    console.log(`╠══════════════════════════════╣`);
    console.log(`║  Passed : ${String(passed).padEnd(19)}║`);
    console.log(`║  Failed : ${String(failed).padEnd(19)}║`);
    console.log(`║  Skipped: ${String(skipped).padEnd(19)}║`);
    console.log(`║  Time   : ${String((durationMs/1000).toFixed(1)+'s').padEnd(19)}║`);
    console.log(`╚══════════════════════════════╝\n`);
  }
}

module.exports = SqlReporter;