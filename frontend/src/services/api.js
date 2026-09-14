// src/services/api.js
//
// Domain API calls, replacing the old src/services.js. Every function here
// is a thin wrapper over apiClient (base URL + auth handled there) - same
// shape as the old file so page components barely change, just their
// import path (`../services` -> `../services/api`).
import apiClient from './apiClient';

// ── Auth ─────────────────────────────────
// Login/refresh/logout orchestration itself lives in AuthContext (it needs
// to update React state, not just call the API) - these two are the raw
// HTTP calls it wraps. Note the snake_case request bodies (user_name, not
// userName) - see apiClient.js's own note on why.
export const login = (userName, password) =>
  apiClient.post('/auth/login', { user_name: userName, password }).then((res) => res.data);

export const logout = (refreshToken) =>
  apiClient.post('/auth/logout', { refresh_token: refreshToken }).then((res) => res.data);

// ── Runs ────────────────────────────────
export const getRuns = () => apiClient.get('/runs').then((res) => res.data);

export const getRunDetail = (id) => apiClient.get(`/runs/${id}`).then((res) => res.data);

export const getSummary = () => apiClient.get('/runs/stats/summary').then((res) => res.data);

export const getTrend = () => apiClient.get('/runs/stats/trend').then((res) => res.data);

// ── Tests ───────────────────────────────
export const getFailedTests = () => apiClient.get('/tests/failed').then((res) => res.data);

export const getSlowestTests = () => apiClient.get('/tests/slowest').then((res) => res.data);

export const getTestsByRun = (runId) =>
  apiClient.get(`/tests/by-run/${runId}`).then((res) => res.data);

export const getTestDetail = (id) => apiClient.get(`/tests/${id}`).then((res) => res.data);

// ── Manual Run ──────────────────────────
export const startRun = (env = {}) =>
  apiClient.post('/manual/run', { env }).then((res) => res.data);

export const stopRun = () => apiClient.post('/manual/stop').then((res) => res.data);

export const runStatus = () => apiClient.get('/manual/status').then((res) => res.data);

// ── AI Analysis ──────────────────────────
export const getExistingAnalysis = (testCaseId) =>
  apiClient
    .get(`/analysis/${testCaseId}`)
    .then((res) => res.data)
    .catch((err) => {
      if (err.response?.status === 404) return null;
      throw err;
    });

export const analyzeTest = (id) =>
  apiClient.post(`/analysis/analyze/${id}`).then((res) => res.data);
