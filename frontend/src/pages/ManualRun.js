// src/pages/ManualRun.js
import React, { useState, useEffect, useRef } from 'react';
import { socket, startRun, stopRun } from '../services';

export default function ManualRun() {
  const [isRunning, setIsRunning] = useState(false);
  const [logs, setLogs] = useState([]);
  const [status, setStatus] = useState(null);
  const logRef = useRef(null);

  useEffect(() => {
    socket.on('run:started', () => {
      setIsRunning(true);
      setStatus({ type: 'running', text: '⏳ Test is running...' });
      setLogs([]);
    });

    socket.on('run:log', ({ log, isError }) => {
      setLogs(prev => [
        ...prev,
        { text: log.trim(), isError, id: Date.now() + Math.random() }
      ]);
    });

    socket.on('run:finished', ({ success }) => {
      setIsRunning(false);
      setStatus({
        type: success ? 'success' : 'error',
        text: success
          ? '✅ All tests completed successfully!'
          : '❌ Some tests have failed.',
      });
    });

    socket.on('run:stopped', () => {
      setIsRunning(false);
      setStatus({ type: 'warning', text: '⛔ Run stopped.' });
    });

    socket.on('run:error', ({ message }) => {
      setIsRunning(false);
      setStatus({ type: 'error', text: `❌ Error: ${message}` });
    });

    return () => {
      socket.off('run:started');
      socket.off('run:log');
      socket.off('run:finished');
      socket.off('run:stopped');
      socket.off('run:error');
    };
  }, []);

  // Auto-scroll logs
  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [logs]);

  const handleStart = async () => {
    try {
      await startRun({ NODE_ENV: 'test' });
    } catch (err) {
      setStatus({
        type: 'error',
        text: `Error: ${err.response?.data?.message || err.message}`,
      });
    }
  };

  const handleStop = async () => {
    await stopRun();
  };

  return (
    <div className="page">
      <h1 className="page-title">Manual Run</h1>

      <div className="card">
        <p style={{ color: '#555', marginBottom: 20 }}>
          Click the button below to start Playwright tests. Live logs will appear in the console.
        </p>

        <div style={{ display: 'flex', gap: 12 }}>
          <button
            className={`btn btn-primary ${isRunning ? 'disabled' : ''}`}
            onClick={handleStart}
            disabled={isRunning}
          >
            {isRunning ? '⏳ Running...' : '▶️ Run Now'}
          </button>

          {isRunning && (
            <button className="btn btn-danger" onClick={handleStop}>
              ⛔ Stop Run
            </button>
          )}
        </div>

        {status && (
          <div className={`alert alert-${status.type}`} style={{ marginTop: 16 }}>
            {status.text}
          </div>
        )}
      </div>

      {/* Live log console */}
      <div className="card">
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: 12,
          }}
        >
          <h2>Live Logs</h2>

          <button
            className="btn btn-sm"
            onClick={() => setLogs([])}
            disabled={isRunning}
          >
            Clear
          </button>
        </div>

        <div
          ref={logRef}
          style={{
            background: '#1a1a2e',
            color: '#e0e0e0',
            fontFamily: 'monospace',
            fontSize: 13,
            padding: '16px',
            borderRadius: 8,
            height: 400,
            overflowY: 'auto',
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-word',
          }}
        >
          {logs.length === 0 ? (
            <span style={{ color: '#555' }}>
              Live logs will appear here when the run starts...
            </span>
          ) : (
            logs.map(log => (
              <div
                key={log.id}
                style={{
                  color: log.isError ? '#ff6b6b' : '#e0e0e0',
                  marginBottom: 2,
                }}
              >
                {log.text}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}