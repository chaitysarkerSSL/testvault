import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  LineChart, Line, XAxis, YAxis, Tooltip,
  ResponsiveContainer, CartesianGrid, Legend,
} from 'recharts';
import { getRuns, getSummary } from '../services';

export default function Dashboard() {
  const [summary, setSummary] = useState(null);
  const [runs,    setRuns]    = useState([]);

  useEffect(() => {
    getSummary().then(setSummary).catch(console.error);
    getRuns().then(setRuns).catch(console.error);
  }, []);

  // Backend থেকে আসা field নাম: total_runs, total_passed, total_failed, avg_pass_rate
  const today = summary?.today || {
    total_runs:    0,
    total_passed:  0,
    total_failed:  0,
    avg_pass_rate: 0,
  };

  const trend = summary?.trend || [];

  return (
    <div className="page">
      <h1 className="page-title">Dashboard</h1>

      {/* Stats Cards */}
      <div className="stats-grid">

        <div className="stat-card">
          <div className="stat-value">{today.total_runs ?? '—'}</div>
          <div className="stat-label">Today's Runs</div>
        </div>

        <div className="stat-card green">
          <div className="stat-value">{today.total_passed ?? '—'}</div>
          <div className="stat-label">Passed</div>
        </div>

        <div className="stat-card red">
          <div className="stat-value">{today.total_failed ?? '—'}</div>
          <div className="stat-label">Failed</div>
        </div>

        <div className="stat-card blue">
          <div className="stat-value">
            {(() => {
              const p = today.total_passed || 0;
              const f = today.total_failed || 0;
              const total = p + f;
              return total > 0 ? `${Math.round((p / total) * 100)}%` : '—';
            })()}
          </div>
          <div className="stat-label">Pass Rate</div>
        </div>

      </div>

      {/* Pass Rate Trend Chart */}
      <div className="card">
        <h2>Pass Rate Trend — Last 14 Days</h2>
        <ResponsiveContainer width="100%" height={220}>
          <LineChart data={trend}>
            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
            <XAxis
              dataKey="run_date"
              tick={{ fontSize: 12 }}
              tickFormatter={(v) =>
                new Date(v).toLocaleDateString('en-GB', { month: 'short', day: 'numeric' })
              }
            />
            <YAxis domain={[0, 100]} tick={{ fontSize: 12 }} unit="%" />
            <Tooltip
              formatter={(v, name) => [
                name === 'pass_rate' ? `${Math.round(v)}%` : v,
                name === 'pass_rate' ? 'Pass Rate' : 'Failures',
              ]}
              labelFormatter={(v) => new Date(v).toLocaleDateString('en-GB')}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="pass_rate"
              name="Pass Rate"
              stroke="#4f46e5"
              strokeWidth={2}
              dot={{ r: 4 }}
            />
            <Line
              type="monotone"
              dataKey="failures"  // ✅ backend থেকে 'failures' আসে
              name="Failures"
              stroke="#ef4444"
              strokeWidth={2}
              dot={{ r: 4 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Recent Runs Table */}
      <div className="card">
        <h2>Recent Runs</h2>
        <table className="table">
          <thead>
            <tr>
              <th>Run ID</th>
              <th>Started At</th>
              <th>Status</th>
              <th>Passed</th>
              <th>Failed</th>
              <th>Duration</th>
            </tr>
          </thead>
          <tbody>
            {runs.map((run) => (
              <tr key={run.id}>
                <td>
                  <Link to={`/runs/${run.id}`} className="link">
                    {run.id.slice(0, 8)}…
                  </Link>
                </td>
                <td>
                  {run.started_at
                    ? new Date(run.started_at).toLocaleString()
                    : '—'}
                </td>
                <td>
                  <span className={`badge ${run.status}`}>
                    {run.status === 'passed'  ? '✅ Passed'  :
                     run.status === 'failed'  ? '❌ Failed'  :
                     '⏳ Running'}
                  </span>
                </td>
                <td className="green-text">{run.passed ?? 0}</td>
                <td className="red-text">{run.failed  ?? 0}</td>
                <td>
                  {run.duration_ms
                    ? `${(run.duration_ms / 1000).toFixed(1)}s`
                    : '—'}
                </td>
              </tr>
            ))}
            {runs.length === 0 && (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', color: '#888' }}>
                  No runs found
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}