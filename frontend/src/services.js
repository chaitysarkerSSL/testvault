import axios from 'axios';
import { io } from 'socket.io-client';

const API = axios.create({
  baseURL: 'http://localhost:4000/api'
});

// ── Socket ─────────────────────────────
export const socket = io('http://localhost:4000', {
  autoConnect: true,
  reconnection: true,
});

// ── Runs ────────────────────────────────
export const getRuns = () =>
  API.get('/runs').then(res => res.data);

export const getRunDetail = (id) =>
  API.get(`/runs/${id}`).then(res => res.data);

export const getSummary = () =>
  API.get('/runs/stats/summary').then(res => res.data);

// ── Tests ───────────────────────────────
export const getFailedTests = () =>
  API.get('/tests/failed').then(res => res.data);

export const getSlowestTests = () =>
  API.get('/tests/slowest').then(res => res.data);

// ── Manual Run ──────────────────────────
export const startRun = (env = {}) =>
  API.post('/manual/run', { env }).then(res => res.data);

export const stopRun = () =>
  API.post('/manual/stop').then(res => res.data);

export const runStatus = () =>
  API.get('/manual/status').then(res => res.data);

// ── AI Analysis ──────────────────────────
export const analyzeTest = (id) =>
  API.post(`/analysis/analyze/${id}`).then(res => res.data);