const router = require('express').Router();
const { getPool, sql } = require('../db');

// ─────────────────────────────
// DASHBOARD SUMMARY
// ─────────────────────────────
router.get('/stats/summary', async (req, res) => {
  try {
    const db = await getPool();

    // Today stats + pass rate
    const todayResult = await db.request().query(`
      SELECT
        COUNT(*)                    AS total_runs,
        ISNULL(SUM(passed),  0)     AS total_passed,
        ISNULL(SUM(failed),  0)     AS total_failed,
        CASE
          WHEN ISNULL(SUM(total), 0) = 0 THEN 0
          ELSE ROUND(
            SUM(passed) * 100.0 / NULLIF(SUM(total), 0), 1
          )
        END                         AS avg_pass_rate
      FROM test_runs
      WHERE CAST(started_at AS DATE) = CAST(GETDATE() AS DATE)
        AND status != 'running'
    `);

    const today = todayResult.recordset[0] || {
      total_runs:    0,
      total_passed:  0,
      total_failed:  0,
      avg_pass_rate: 0,
    };

    // Last 14 days trend — subquery দিয়ে সঠিক order
    const trendResult = await db.request().query(`
      SELECT *
      FROM (
        SELECT TOP 14
          CAST(started_at AS DATE)  AS run_date,
          ISNULL(SUM(passed), 0)    AS passed,
          ISNULL(SUM(failed), 0)    AS failures,
          CASE
            WHEN ISNULL(SUM(total), 0) = 0 THEN 0
            ELSE ROUND(
              SUM(passed) * 100.0 / NULLIF(SUM(total), 0), 1
            )
          END                       AS pass_rate
        FROM test_runs
        WHERE status != 'running'
        GROUP BY CAST(started_at AS DATE)
        ORDER BY CAST(started_at AS DATE) DESC  -- শেষের ১৪ দিন নাও
      ) t
      ORDER BY run_date ASC  -- Chart এ বাম থেকে ডানে দেখাও
    `);

    res.json({
      today,
      trend: trendResult.recordset,
    });

  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// ─────────────────────────────
// GET ALL RUNS
// ─────────────────────────────
router.get('/', async (req, res) => {
  try {
    const db = await getPool();
    const result = await db.request().query(`
      SELECT TOP 50 * FROM test_runs
      ORDER BY started_at DESC
    `);
    res.json(result.recordset);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// ─────────────────────────────
// GET RUN DETAILS
// ─────────────────────────────
router.get('/:id', async (req, res) => {
  try {
    const db = await getPool();

    const run = await db.request()
      .input('id', sql.VarChar, req.params.id)
      .query('SELECT * FROM test_runs WHERE id = @id');

    const tests = await db.request()
      .input('id', sql.VarChar, req.params.id)
      .query(`
        SELECT
          tc.*,
          aa.root_cause,
          aa.fix_suggestion
        FROM test_cases tc
        LEFT JOIN ai_analysis aa ON aa.test_case_id = tc.id
        WHERE tc.run_id = @id
        ORDER BY tc.created_at ASC
      `);

    if (!run.recordset[0]) {
      return res.status(404).json({ error: 'Run not found' });
    }

    res.json({
      ...run.recordset[0],
      tests: tests.recordset,
    });

  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;