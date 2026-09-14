// src/pages/FailedTests.js
import React, { useEffect, useState } from 'react';
import { useHasRole } from '../context/AuthContext';
import { apiOrigin } from '../services/apiClient';
import { getFailedTests, analyzeTest } from '../services/api';

export default function FailedTests() {
  const canAnalyze = useHasRole('Admin', 'Tester');
  const [tests, setTests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [analyzing, setAnalyzing] = useState({});

  useEffect(() => {
    getFailedTests()
      .then(setTests)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  const handleAnalyze = async (test) => {
    setAnalyzing(prev => ({ ...prev, [test.id]: true }));
    try {
      const result = await analyzeTest(test.id);
      setTests(prev =>
        prev.map(t =>
          t.id === test.id
            ? { ...t, root_cause: result.root_cause, fix_suggestion: result.fix_suggestion }
            : t
        )
      );
    } catch (err) {
      alert('AI analysis failed: ' + err.message);
    } finally {
      setAnalyzing(prev => ({ ...prev, [test.id]: false }));
    }
  };

  if (loading) return <div className="page"><p>Loading...</p></div>;

  return (
    <div className="page">
      <h1 className="page-title">Failed Tests</h1>

      {tests.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', color: '#888' }}>
          No failed tests found 🎉
        </div>
      ) : (
        tests.map(test => (
          <div key={test.id} className="card failed-card">
            <div className="failed-header">
              <span className="failed-title">{test.title}</span>
              <span className="badge failed">❌ Failed</span>
            </div>

            {test.error_message && (
              <div className="error-box">
                <strong>Error:</strong>
                <pre>{test.error_message}</pre>
              </div>
            )}

            <div className="test-meta">
              <span>Browser: {test.browser || 'chromium'}</span>
              <span>Duration: {(test.duration_ms / 1000).toFixed(1)}s</span>
              <span>Retry: {test.retry_count}</span>

              {test.screenshot && (
                <a href={`${apiOrigin}/${test.screenshot}`} target="_blank" rel="noreferrer">
                  📸 Screenshot
                </a>
              )}

              {test.trace && (
                <a href={`${apiOrigin}/${test.trace}`} target="_blank" rel="noreferrer">
                  🔍 Trace
                </a>
              )}
            </div>

            {/* AI Analysis */}
            {test.root_cause ? (
              <div className="ai-analysis">
                <h4>AI Analysis</h4>

                <div className="analysis-section">
                  <strong>Root Cause:</strong>
                  <p>{test.root_cause}</p>
                </div>

                <div className="analysis-section">
                  <strong>Fix Suggestion:</strong>
                  <p>{test.fix_suggestion}</p>
                </div>
              </div>
            ) : (
              canAnalyze && (
                <button
                  className="btn btn-ai"
                  onClick={() => handleAnalyze(test)}
                  disabled={analyzing[test.id]}
                >
                  {analyzing[test.id] ? 'Analyzing...' : 'Run AI Analysis'}
                </button>
              )
            )}
          </div>
        ))
      )}
    </div>
  );
}