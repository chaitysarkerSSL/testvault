// src/pages/ManualRun.js
import React, { useEffect, useRef, useState } from 'react';
import { useHasRole } from '../context/AuthContext';
import { runStatus, startRun, stopRun } from '../services/api';
import { RUN_EVENTS, on, off, start as startSignalR } from '../services/signalRService';

export default function ManualRun() {
  const canOperate = useHasRole('Admin', 'Tester');

  const [isRunning, setIsRunning] = useState(false);
  const [logs, setLogs] = useState([]);
  const [status, setStatus] = useState(null);
  const logRef = useRef(null);

  useEffect(() => {
    // Seed the initial state in case a run is already in progress (e.g. the
    // page was reloaded mid-run) - the original Socket.IO version had no
    // equivalent call and would just show "not running" until the next
    // event happened to arrive.
    runStatus()
      .then((s) => setIsRunning(Boolean(s.is_running)))
      .catch(() => {});

    // Event payloads use SignalR's own default JSON casing (camelCase -
    // runId/isError/exitCode/...), which is NOT the snake_case the REST API
    // uses (see apiClient.js's note) - SignalR's JsonHubProtocol is a
    // separate serializer from the MVC one TestVault.Web configures for
    // controllers.
    const handleStarted = () => {
      setIsRunning(true);
      setStatus({ type: 'running', text: '⏳ Test is running...' });
      setLogs([]);
    };

    const handleLog = ({ log, isError }) => {
      setLogs((prev) => [...prev, { text: log.trim(), isError, id: Date.now() + Math.random() }]);
    };

    const handleFinished = ({ success }) => {
      setIsRunning(false);
      setStatus({
        type: success ? 'success' : 'error',
        text: success ? '✅ All tests completed successfully!' : '❌ Some tests have failed.',
      });
    };

    const handleStopped = () => {
      setIsRunning(false);
      setStatus({ type: 'warning', text: '⛔ Run stopped.' });
    };

    const handleError = ({ message }) => {
      setIsRunning(false);
      setStatus({ type: 'error', text: `❌ Error: ${message}` });
    };

    on(RUN_EVENTS.RUN_STARTED, handleStarted);
    on(RUN_EVENTS.RUN_LOG, handleLog);
    on(RUN_EVENTS.RUN_FINISHED, handleFinished);
    on(RUN_EVENTS.RUN_STOPPED, handleStopped);
    on(RUN_EVENTS.RUN_ERROR, handleError);

    startSignalR().catch((err) => {
      // eslint-disable-next-line no-console
      console.error('Failed to connect to the live run hub:', err);
      setStatus({ type: 'error', text: '❌ Could not connect to live updates. Logs will not stream in real time.' });
    });

    return () => {
      off(RUN_EVENTS.RUN_STARTED, handleStarted);
      off(RUN_EVENTS.RUN_LOG, handleLog);
      off(RUN_EVENTS.RUN_FINISHED, handleFinished);
      off(RUN_EVENTS.RUN_STOPPED, handleStopped);
      off(RUN_EVENTS.RUN_ERROR, handleError);
      // Deliberately not stopping the connection here - it's a shared
      // singleton (services/signalRService.js) other pages may also use.
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
    try {
      await stopRun();
    } catch (err) {
      setStatus({
        type: 'error',
        text: `Error: ${err.response?.data?.message || err.message}`,
      });
    }
  };

  return (
    <div className="page">
      <h1 className="page-title">Manual Run</h1>

      <div className="card">
        {canOperate ? (
          <>
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
          </>
        ) : (
          <p style={{ color: '#888' }}>
            Your role does not permit starting or stopping test runs. You can still watch live
            logs below while a Tester or Admin runs one.
          </p>
        )}

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

          <button className="btn btn-sm" onClick={() => setLogs([])} disabled={isRunning}>
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
            <span style={{ color: '#555' }}>Live logs will appear here when the run starts...</span>
          ) : (
            logs.map((log) => (
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
