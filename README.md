# 🔒 TestVault

Playwright Test Reporting Dashboard — Real-time, AI-powered, এবং সম্পূর্ণ automated।

## Features

- ✅ Playwright test results SQL Server এ save
- 📊 React Dashboard — charts, history, stats
- ⚡ Real-time updates via Socket.IO
- 🤖 AI Failure Analysis (Claude API)
- 📩 Slack / Teams / Email notifications
- ▶️ Dashboard থেকে Manual Run
- ⚙️ Parallel Execution (3 workers)
- ⏰ Scheduled runs (Cron / Task Scheduler)

---

## Project Structure

```
testvault/
├── backend/
│   ├── server.js          ← Express + Socket.IO server
│   ├── db.js              ← SQL Server connection + schema
│   ├── routes/
│   │   ├── runs.js        ← Run history API
│   │   ├── tests.js       ← Test cases API
│   │   ├── manual.js      ← Manual run trigger
│   │   └── analysis.js    ← AI analysis API
│   ├── package.json
│   └── .env.example
├── reporters/
│   ├── sql-reporter.js    ← Custom Playwright Reporter
│   └── notifier.js        ← Slack / Teams / Email
├── scripts/
│   └── run-tests.js       ← Runner script
├── tests/
│   └── example.spec.js    ← Example Playwright tests
├── frontend/
│   ├── src/
│   │   ├── App.js
│   │   ├── App.css
│   │   ├── services.js
│   │   └── pages/
│   │       ├── Dashboard.js
│   │       ├── ManualRun.js
│   │       ├── FailedTests.js
│   │       └── RunDetail.js
│   └── package.json
└── playwright.config.js
```

---

## Setup

### Step 1 — SQL Server

SQL Server এ `TestVault` নামে একটি database তৈরি করুন।
Server চালু হলে schema automatically তৈরি হবে।

### Step 2 — Backend

```bash
cd backend
cp .env.example .env
# .env ফাইলে আপনার credentials দিন
npm install
npm start
```

### Step 3 — Frontend

```bash
cd frontend
npm install
npm start
```

### Step 4 — Playwright Install

```bash
npm install @playwright/test
npx playwright install
```

### Step 5 — Test Run

```bash
# Manual
node scripts/run-tests.js

# অথবা Dashboard থেকে "Run Now" বাটন চাপুন
```

---

## Scheduler Setup

### Windows Task Scheduler

1. Task Scheduler খুলুন
2. "Create Basic Task" → নাম দিন "TestVault Nightly"
3. Trigger: Daily → 02:00 AM
4. Action: Program = `node`, Arguments = `C:\testvault\scripts\run-tests.js`

### Linux Cron

```bash
crontab -e
# প্রতিদিন রাত ২টায়
0 2 * * * /usr/bin/node /home/user/testvault/scripts/run-tests.js >> /var/log/testvault.log 2>&1
# প্রতি ৩০ মিনিটে smoke test
*/30 * * * * /usr/bin/node /home/user/testvault/scripts/run-tests.js --grep smoke
```

---

## Environment Variables (.env)

| Variable | বিবরণ |
|----------|-------|
| `DB_SERVER` | SQL Server hostname |
| `DB_NAME` | Database নাম (TestVault) |
| `DB_USER` | SQL Server username |
| `DB_PASSWORD` | SQL Server password |
| `SLACK_WEBHOOK_URL` | Slack notification webhook |
| `TEAMS_WEBHOOK_URL` | Microsoft Teams webhook |
| `SMTP_USER` | Gmail address |
| `SMTP_PASS` | Gmail app password |
| `NOTIFY_EMAIL` | যেখানে email যাবে |
| `ANTHROPIC_API_KEY` | Claude API key (AI analysis) |
| `TEST_BASE_URL` | আপনার app এর URL |

---

## URLs

| Service | URL |
|---------|-----|
| Backend API | http://localhost:4000 |
| Dashboard | http://localhost:3000 |
| Screenshots | http://localhost:4000/screenshots |
