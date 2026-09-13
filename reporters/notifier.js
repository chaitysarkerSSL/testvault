// reporters/notifier.js — Slack, Teams, Email notifications
require('dotenv').config();
const axios      = require('axios');
const nodemailer = require('nodemailer');

async function sendNotification({ runId, passed, failed, skipped, status, durationMs }) {
  const icon     = status === 'passed' ? '✅' : '❌';
  const passRate = Math.round((passed / (passed + failed + skipped || 1)) * 100);
  const duration = (durationMs / 1000).toFixed(1);

  const promises = [];

  // ── Slack ──────────────────────────────────────────────────
  if (process.env.SLACK_WEBHOOK_URL) {
    promises.push(
      axios.post(process.env.SLACK_WEBHOOK_URL, {
        blocks: [
          {
            type: 'header',
            text: { type: 'plain_text', text: `${icon} TestVault — Run ${status.toUpperCase()}` },
          },
          {
            type: 'section',
            fields: [
              { type: 'mrkdwn', text: `*Passed:*\n${passed}`    },
              { type: 'mrkdwn', text: `*Failed:*\n${failed}`    },
              { type: 'mrkdwn', text: `*Pass Rate:*\n${passRate}%` },
              { type: 'mrkdwn', text: `*Duration:*\n${duration}s`  },
            ],
          },
          {
            type: 'context',
            elements: [{ type: 'mrkdwn', text: `Run ID: \`${runId}\`` }],
          },
        ],
      }).catch(e => console.warn('[Slack]', e.message))
    );
  }

  // ── Microsoft Teams ────────────────────────────────────────
  if (process.env.TEAMS_WEBHOOK_URL) {
    promises.push(
      axios.post(process.env.TEAMS_WEBHOOK_URL, {
        '@type':    'MessageCard',
        '@context': 'http://schema.org/extensions',
        themeColor: status === 'passed' ? '00B050' : 'FF0000',
        summary:    `TestVault Run ${status}`,
        sections: [{
          activityTitle: `${icon} TestVault — Run ${status.toUpperCase()}`,
          facts: [
            { name: 'Passed',    value: String(passed)   },
            { name: 'Failed',    value: String(failed)   },
            { name: 'Pass Rate', value: `${passRate}%`   },
            { name: 'Duration',  value: `${duration}s`   },
            { name: 'Run ID',    value: runId            },
          ],
        }],
      }).catch(e => console.warn('[Teams]', e.message))
    );
  }

  // ── Email ──────────────────────────────────────────────────
  if (process.env.SMTP_USER && process.env.NOTIFY_EMAIL) {
    const transporter = nodemailer.createTransport({
      host:   process.env.SMTP_HOST || 'smtp.gmail.com',
      port:   parseInt(process.env.SMTP_PORT) || 587,
      secure: false,
      auth: {
        user: process.env.SMTP_USER,
        pass: process.env.SMTP_PASS,
      },
    });

    promises.push(
      transporter.sendMail({
        from:    `"TestVault" <${process.env.SMTP_USER}>`,
        to:      process.env.NOTIFY_EMAIL,
        subject: `${icon} TestVault Run ${status.toUpperCase()} — ${passRate}% passed`,
        html: `
          <h2 style="color:${status === 'passed' ? '#00B050' : '#FF0000'}">
            ${icon} TestVault — Run ${status.toUpperCase()}
          </h2>
          <table style="border-collapse:collapse;font-family:sans-serif">
            <tr>
              <td style="padding:8px;border:1px solid #ddd"><b>Passed</b></td>
              <td style="padding:8px;border:1px solid #ddd;color:green">${passed}</td>
            </tr>
            <tr>
              <td style="padding:8px;border:1px solid #ddd"><b>Failed</b></td>
              <td style="padding:8px;border:1px solid #ddd;color:red">${failed}</td>
            </tr>
            <tr>
              <td style="padding:8px;border:1px solid #ddd"><b>Skipped</b></td>
              <td style="padding:8px;border:1px solid #ddd">${skipped}</td>
            </tr>
            <tr>
              <td style="padding:8px;border:1px solid #ddd"><b>Pass Rate</b></td>
              <td style="padding:8px;border:1px solid #ddd">${passRate}%</td>
            </tr>
            <tr>
              <td style="padding:8px;border:1px solid #ddd"><b>Duration</b></td>
              <td style="padding:8px;border:1px solid #ddd">${duration}s</td>
            </tr>
            <tr>
              <td style="padding:8px;border:1px solid #ddd"><b>Run ID</b></td>
              <td style="padding:8px;border:1px solid #ddd">${runId}</td>
            </tr>
          </table>
        `,
      }).catch(e => console.warn('[Email]', e.message))
    );
  }

  if (promises.length === 0) {
    console.log('[TestVault] Notification skip — .env এ কোনো webhook/email set নেই।');
    return;
  }

  await Promise.allSettled(promises);
  console.log('[TestVault] Notifications sent.');
}

module.exports = { sendNotification };