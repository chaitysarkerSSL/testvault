// server.js — TestVault Main Server
require('dotenv').config();
const express    = require('express');
const http       = require('http');
const { Server } = require('socket.io');
const cors       = require('cors');
const { getPool } = require('./db');

const app    = express();
const server = http.createServer(app);

const io = new Server(server, {
  cors: {
    origin: process.env.FRONTEND_URL || 'http://localhost:3000',
    methods: ['GET', 'POST'],
  },
});

// ── Middleware ──────────────────────────────────────────────
app.use(cors({ origin: process.env.FRONTEND_URL || 'http://localhost:3000' }));
app.use(express.json());
app.use('/screenshots', express.static('screenshots'));
app.use('/videos',      express.static('videos'));
app.use('/traces',      express.static('traces'));

// ── Share io instance with routes ──────────────────────────
app.set('io', io);

// ── Routes ─────────────────────────────────────────────────
app.use('/api/runs',    require('./routes/runs'));
app.use('/api/tests',   require('./routes/tests'));
app.use('/api/manual',  require('./routes/manual'));
app.use('/api/analysis',require('./routes/analysis'));

// ── Socket.IO ──────────────────────────────────────────────
io.on('connection', (socket) => {
  console.log('[Socket] Client connected:', socket.id);
  socket.on('disconnect', () => {
    console.log('[Socket] Client disconnected:', socket.id);
  });
});

// ── Start ───────────────────────────────────────────────────
const PORT = process.env.PORT || 4000;

async function start() {
  await getPool();
  server.listen(PORT, () => {
    console.log(`[TestVault] Server running → http://localhost:${PORT}`);
  });
}

start();