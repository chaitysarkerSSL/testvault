// routes/analysis.js — AI Failure Analysis via Claude API
const router = require('express').Router();
const axios  = require('axios');
const { getPool, sql } = require('../db');

router.post('/analyze/:testCaseId', async (req, res) => {
  try {
    const db = await getPool();

    // Failed test এর details আনো
    const result = await db.request()
      .input('id', sql.Int, req.params.testCaseId)
      .query('SELECT * FROM test_cases WHERE id = @id');

    const test = result.recordset[0];
    if (!test) return res.status(404).json({ error: 'Test not found' });
    if (test.status !== 'failed') {
      return res.status(400).json({ error: 'শুধু failed test analyze করা যাবে।' });
    }

    // Claude API call
    const prompt = `
তুমি একজন QA Engineer। নিচের Playwright test failure টি analyze করো।

Test Name: ${test.title}
Error Message: ${test.error_message || 'N/A'}
Browser: ${test.browser || 'chromium'}
Duration: ${test.duration_ms}ms
Retry Count: ${test.retry_count}

নিচের format এ JSON দাও (শুধু JSON, অন্য কিছু না):
{
  "root_cause": "সম্ভাব্য কারণ বাংলায় ২-৩ বাক্যে",
  "fix_suggestion": "কীভাবে ঠিক করবে বাংলায় ধাপে ধাপে"
}
    `.trim();

    const aiRes = await axios.post(
      'https://api.anthropic.com/v1/messages',
      {
        model: 'claude-sonnet-4-6',
        max_tokens: 1000,
        messages: [{ role: 'user', content: prompt }],
      },
      {
        headers: {
          'x-api-key': process.env.ANTHROPIC_API_KEY,
          'anthropic-version': '2023-06-01',
          'content-type': 'application/json',
        },
      }
    );

    const raw = aiRes.data.content[0].text.trim();
    const clean = raw.replace(/```json|```/g, '').trim();
    const analysis = JSON.parse(clean);

    // Save to DB
    await db.request()
      .input('testCaseId', sql.Int, test.id)
      .input('rootCause', sql.NVarChar, analysis.root_cause)
      .input('fixSuggestion', sql.NVarChar, analysis.fix_suggestion)
      .query(`
        MERGE ai_analysis AS target
        USING (VALUES (@testCaseId)) AS source(test_case_id)
        ON target.test_case_id = source.test_case_id
        WHEN MATCHED THEN
          UPDATE SET root_cause = @rootCause, fix_suggestion = @fixSuggestion, analyzed_at = GETDATE()
        WHEN NOT MATCHED THEN
          INSERT (test_case_id, root_cause, fix_suggestion)
          VALUES (@testCaseId, @rootCause, @fixSuggestion);
      `);

    res.json(analysis);
  } catch (err) {
    console.error('[AI Analysis Error]', err.message);
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;
