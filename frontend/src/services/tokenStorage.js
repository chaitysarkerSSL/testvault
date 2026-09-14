// src/services/tokenStorage.js
//
// Plain (non-React) module owning where the JWT access/refresh token pair
// actually lives, shared by apiClient.js (attaches the access token to
// every request, persists a new pair after a refresh) and AuthContext.js
// (the React-facing wrapper around this same state). Kept separate from
// AuthContext specifically so apiClient never has to import React context -
// it just calls these plain functions.
//
// Storage split, and the tradeoff that comes with it:
//   - Access token: an in-memory variable only. Never touches disk, so it
//     can't be read back out of localStorage/sessionStorage by an XSS
//     payload after the fact - but it's also lost on every page reload,
//     which is why AuthContext does a silent refresh on startup.
//   - Refresh token: localStorage, so a reload/tab-close doesn't force a
//     fresh login every time. This is the standard pragmatic choice for a
//     pure SPA with no backend-for-frontend - it IS still readable by any
//     script running on the page (a real XSS vulnerability could steal it),
//     which an httpOnly-cookie-based flow would avoid. That would require
//     the backend to issue cookies instead of a JSON body (Phase 6 does
//     the latter), so it's a real gap worth knowing about, not a silent one -
//     just out of scope for this phase, which integrates with the API
//     Phase 6 actually built.

const REFRESH_TOKEN_KEY = 'testvault.refreshToken';

let accessToken = null;
let accessTokenExpiresAt = null;

export function getAccessToken() {
  return accessToken;
}

export function getAccessTokenExpiresAt() {
  return accessTokenExpiresAt;
}

export function setAccessToken(token, expiresAt) {
  accessToken = token || null;
  accessTokenExpiresAt = expiresAt ? new Date(expiresAt) : null;
}

export function getRefreshToken() {
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  } catch {
    // localStorage can throw in some contexts (private browsing in older
    // Safari, disabled storage) - degrade to "no persisted session" rather
    // than crash the app.
    return null;
  }
}

export function setRefreshToken(token) {
  try {
    if (token) {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } else {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
    }
  } catch {
    // See getRefreshToken - best effort only.
  }
}

/** Persists a fresh { accessToken, accessTokenExpiresAt, refreshToken } triple, e.g. after login or a refresh. */
export function setSession({ accessToken: at, accessTokenExpiresAt: ate, refreshToken: rt }) {
  setAccessToken(at, ate);
  setRefreshToken(rt);
}

/** Clears everything - logout, or a refresh attempt that failed for good. */
export function clearSession() {
  setAccessToken(null, null);
  setRefreshToken(null);
}

/**
 * Decodes a JWT's payload without verifying its signature - this is only
 * ever used to read the (already server-validated, HTTPS-delivered) access
 * token's own claims for UI purposes (username, roles), never to trust an
 * externally-supplied token as authentic. Real verification happens
 * entirely server-side on every request.
 */
export function decodeJwt(token) {
  try {
    const payload = token.split('.')[1];
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join('')
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}
