require('dotenv').config();
const sql = require('mssql');

const config = {
  server:   process.env.DB_SERVER   || 'SSL-DEV',
  database: process.env.DB_NAME     || 'TestVault',
  user:     process.env.DB_USER     || 'sa',
  password: process.env.DB_PASSWORD || 'S123456_',
  port:     parseInt(process.env.DB_PORT || '1433'),
  options: {
    encrypt:                false,
    trustServerCertificate: true,
    enableArithAbort:       true,
  },
  authentication: { type: 'default' },
  pool: { max: 10, min: 0, idleTimeoutMillis: 30000 },
};

let pool = null;

async function getPool() {
  if (!pool) {
    pool = await sql.connect(config);
    console.log('[TestVault] SQL Server Connected Successfully');
  }
  return pool;
}

async function closePool() {
  if (pool) {
    await pool.close();
    pool = null;
    console.log('[TestVault] SQL Connection Closed');
  }
}

module.exports = { sql, getPool, closePool };