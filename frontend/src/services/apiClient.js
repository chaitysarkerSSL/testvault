// src/services/apiClient.js
//
// Centralized HTTP client for the ASP.NET Core API (replacing the raw
// axios.create({ baseURL: 'http://localhost:4000/api' }) that used to live
// in src/services.js). Owns exactly the cross-cutting concerns a "shared
// API client" is for - base URL, attaching the access token, and
// transparently recovering from an expired one - so every page/component
// keeps calling plain functions (see services/api.js) with no auth
// plumbing of its own.
//
// IMPORTANT: this API's JSON uses snake_case property names throughout
// (total_runs, root_cause, ... - see TestVault.Web's SnakeCaseJsonNamingPolicy),
// and that applies to REQUEST bodies too, not just responses - a body of
// { userName: ... } would silently fail to bind server-side. Every request
// body built in this file (and in services/api.js) uses snake_case for
// exactly that reason.
import axios from 'axios';
import { getAccessToken, getRefreshToken, setSession, clearSession } from './tokenStorage';

const API_URL = process.env.REACT_APP_API_URL;

if (!API_URL) {
  // Fails loudly in the browser console instead of silently sending every
  // request to a relative (now-wrong, since the CRA `proxy` field was
  // removed in favor of this) path - mirrors the backend's own fail-fast
  // posture for a missing Jwt:Secret.
  // eslint-disable-next-line no-console
  console.error(
    'REACT_APP_API_URL is not set - see frontend/.env.development. API calls will fail.'
  );
}

/** The bare API origin (no /api suffix) - reused by signalRService.js for the hub URL and by pages for screenshot/trace/video links. */
export const apiOrigin = API_URL || '';

const apiClient = axios.create({
  baseURL: `${apiOrigin}/api`,
});

// A second, deliberately plain axios instance for the one call (token
// refresh) that must never itself go through the 401-retry interceptor
// below - that would recurse.
const refreshClient = axios.create({
  baseURL: `${apiOrigin}/api`,
});

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let sessionExpiredHandlers = [];

/**
 * Registered by AuthContext (once) so this module can announce "the
 * session is gone" (refresh failed, or there was nothing to refresh)
 * without importing React/react-router itself.
 */
export function onSessionExpired(handler) {
  sessionExpiredHandlers.push(handler);
  return () => {
    sessionExpiredHandlers = sessionExpiredHandlers.filter((h) => h !== handler);
  };
}

function announceSessionExpired() {
  clearSession();
  sessionExpiredHandlers.forEach((handler) => handler());
}

// Only one refresh may ever be in flight at a time. This isn't just an
// efficiency nicety: the backend's refresh tokens are single-use and
// rotate on every call (AuthService.RefreshTokenAsync) - presenting an
// already-used token a second time is treated as a theft signal and
// revokes EVERY session for that user. If several requests 401 at once
// (e.g. a batch of dashboard calls right after the access token expired),
// they must all await the same in-flight refresh and share its result,
// never each start their own.
let refreshPromise = null;

async function refreshAccessToken() {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    throw new Error('No refresh token available.');
  }

  const { data } = await refreshClient.post('/auth/refresh', { refresh_token: refreshToken });

  setSession({
    accessToken: data.access_token,
    accessTokenExpiresAt: data.access_token_expires_at,
    refreshToken: data.refresh_token,
  });

  return data.access_token;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const { response, config } = error;

    // Not a 401, or we already retried this exact request once - give up.
    if (!response || response.status !== 401 || !config || config._retriedAfterRefresh) {
      return Promise.reject(error);
    }

    if (!getRefreshToken()) {
      // Nothing to refresh with - this really is "logged out", not
      // "briefly expired".
      announceSessionExpired();
      return Promise.reject(error);
    }

    config._retriedAfterRefresh = true;

    try {
      if (!refreshPromise) {
        refreshPromise = refreshAccessToken().finally(() => {
          refreshPromise = null;
        });
      }

      const newAccessToken = await refreshPromise;
      config.headers.Authorization = `Bearer ${newAccessToken}`;
      return apiClient(config);
    } catch (refreshError) {
      // The refresh token itself was rejected (expired/revoked/reused) -
      // there's no recovering from this without a fresh login.
      announceSessionExpired();
      return Promise.reject(refreshError);
    }
  }
);

export default apiClient;
