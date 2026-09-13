// routes/manual.js — Manual Run trigger
const router  = require('express').Router();
const { spawn } = require('child_process');
const path    = require('path');

let activeProcess = null;

// Manual run শুরু করো
router.post('/run', (req, res) => {
  if (activeProcess) {
    return res.status(409).json({
      message: 'একটি test run ইতিমধ্যে চলছে। আগে শেষ হোক।',
    });
  }

  const io = req.app.get('io');
  const env = req.body.env || {};

  res.json({ message: 'TestVault run শুরু হয়েছে!' });

  // Run শুরু
  io.emit('run:started', { startedAt: new Date() });

  const scriptPath = path.join(__dirname, '..', '..', 'scripts', 'run-tests.js');

  activeProcess = spawn('node', [scriptPath], {
    env: {
      ...process.env,
      ...env,
      TRIGGERED_BY: 'manual',
    },
  });

  // Live log পাঠাও
  activeProcess.stdout.on('data', (data) => {
    const log = data.toString();
    console.log('[Runner]', log);
    io.emit('run:log', { log, isError: false, time: new Date() });
  });

  activeProcess.stderr.on('data', (data) => {
    const log = data.toString();
    console.error('[Runner Error]', log);
    io.emit('run:log', { log, isError: true, time: new Date() });
  });

  activeProcess.on('close', (code) => {
    activeProcess = null;
    const success = code === 0;
    console.log(`[Runner] Finished with code ${code}`);
    io.emit('run:finished', {
      success,
      exitCode: code,
      finishedAt: new Date(),
    });
  });

  activeProcess.on('error', (err) => {
    activeProcess = null;
    io.emit('run:error', { message: err.message });
  });
});

// চলছে কিনা চেক করো
router.get('/status', (req, res) => {
  res.json({ isRunning: activeProcess !== null });
});

// চলমান run বন্ধ করো
router.post('/stop', (req, res) => {
  if (!activeProcess) {
    return res.status(400).json({ message: 'কোনো run চলছে না।' });
  }
  activeProcess.kill('SIGTERM');
  activeProcess = null;
  req.app.get('io').emit('run:stopped', { stoppedAt: new Date() });
  res.json({ message: 'Run বন্ধ করা হয়েছে।' });
});

module.exports = router;
