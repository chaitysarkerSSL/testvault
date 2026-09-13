// src/pages/RunDetail.js
import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getRunDetail } from '../services';

export default function RunDetail() {
  const { id } = useParams();
  const [run, setRun] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getRunDetail(id)
      .then(setRun)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="page"><p>Loading...</p></div>;
  if (!run) return <div className="page"><p>Run not found.</p></div>;

  const passRate = run.total
    ? Math.round((run.passed / run.total) * 100)
    : 0;

  return (
    <div className="page">
      <Link to="/" className="back-link">← Dashboard</Link>

      <h1 className="page-title">Run Details</h1>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-value">{run.total}</div>
          <div className="stat-label">Total</div>
        </div>

        <div className="stat-card green">
          <div className="stat-value">{run.passed}</div>
          <div className="stat-label">Passed</div>
        </div>

        <div className="stat-card red">
          <div className="stat-value">{run.failed}</div>
          <div className="stat-label">Failed</div>
        </div>

        <div className="stat-card blue">
          <div className="stat-value">{passRate}%</div>
          <div className="stat-label">Pass Rate</div>
        </div>
      </div>

      <div className="card">
        <div className="test-meta" style={{ marginBottom: 0 }}>
          <span>Run ID: <code>{run.id}</code></span>
          <span>Triggered by: {run.triggered_by}</span>
          <span>Browser: {run.browser}</span>
          <span>
            Started: {new Date(run.started_at).toLocaleString()}
          </span>
          <span>
            Duration: {run.duration_ms
              ? `${(run.duration_ms / 1000).toFixed(1)}s`
              : '—'}
          </span>
        </div>
      </div>

      <div className="card">
        <h2>Test Cases</h2>

        <table className="table">
          <thead>
            <tr>
              <th>Test Name</th>
              <th>Status</th>
              <th>Duration</th>
              <th>Browser</th>
              <th>Worker</th>
              <th>Artifacts</th>
            </tr>
          </thead>

          <tbody>
            {(run.tests || []).map(t => (
              <tr key={t.id}>
                <td style={{ maxWidth: 340, wordBreak: 'break-word' }}>
                  {t.title}
                </td>

                <td>
                  <span className={`badge ${t.status}`}>
                    {t.status === 'passed'
                      ? '✅ passed'
                      : t.status === 'failed'
                      ? '❌ failed'
                      : '⏭ skipped'}
                  </span>
                </td>

                <td>{(t.duration_ms / 1000).toFixed(1)}s</td>
                <td>{t.browser || '—'}</td>
                <td>#{t.worker_index}</td>

                <td>
                  {t.screenshot && (
                    <a
                      href={`http://localhost:4000/${t.screenshot}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      📸 Screenshot
                    </a>
                  )}

                  {t.trace && (
                    <a
                      href={`http://localhost:4000/${t.trace}`}
                      target="_blank"
                      rel="noreferrer"
                      style={{ marginLeft: 8 }}
                    >
                      🔍 Trace
                    </a>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}