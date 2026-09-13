// routes/tests.js — Individual test case API
const router = require('express').Router();
const { getPool, sql } = require('../db');

// Failed tests list
router.get('/failed', async (req, res) => {
  try {
    const db = await getPool();
    const result = await db.request().query(`
      SELECT TOP 20
        tc.*,
        tr.started_at AS run_started_at,
        aa.root_cause,
        aa.fix_suggestion
      FROM test_cases tc
      JOIN test_runs tr ON tr.id = tc.run_id
      LEFT JOIN ai_analysis aa ON aa.test_case_id = tc.id
      WHERE tc.status = 'failed'
      ORDER BY tc.created_at DESC
    `);
    res.json(result.recordset);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// Slowest tests
router.get('/slowest', async (req, res) => {
  try {
    const db = await getPool();
    const result = await db.request().query(`
      SELECT TOP 10 title, AVG(duration_ms) AS avg_ms, COUNT(*) AS run_count
      FROM test_cases
      GROUP BY title
      ORDER BY avg_ms DESC
    `);
    res.json(result.recordset);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;
