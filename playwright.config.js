// playwright.config.js
const { defineConfig, devices } = require('@playwright/test');
require('dotenv').config();

module.exports = defineConfig({
  testDir: './tests',
  fullyParallel: true,
  workers: 3,             // Parallel execution — 3 worker
  retries: 1,             // Fail হলে একবার retry
  timeout: 30_000,        // প্রতি test এর max time

  use: {
    baseURL:       process.env.TEST_BASE_URL || 'https://dev-hospital.shampanlab.com',
    screenshot:    'only-on-failure',
    video:         'retain-on-failure',
    trace:         'on-first-retry',
    headless:      true,
  },

  reporter: [
    ['./reporters/sql-reporter.js'],  // SQL Server এ save
    ['list'],                          // Console এ দেখাও
  ],

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    /*{
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },*/
  ],
});