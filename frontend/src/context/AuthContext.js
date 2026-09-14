// src/context/AuthContext.js
//
// React-facing session state: who's logged in, what roles they have, and
// login()/logout(). Wraps services/tokenStorage.js (the actual token
// storage, shared with apiClient.js) rather than owning storage itself.
import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { login as loginApi, logout as logoutApi } from '../services/api';
import apiClient, { onSessionExpired } from '../services/apiClient';
import { reset as resetSignalR } from '../services/signalRService';
import {
  clearSession,
  decodeJwt,
  getAccessToken,
  getRefreshToken,
  setSession,
} from '../services/tokenStorage';

const AuthContext = createContext(null);

function userFromAccessToken(accessToken) {
  if (!accessToken) return null;

  const claims = decodeJwt(accessToken);
  if (!claims) return null;

  // Matches JwtTokenService's claim names exactly (sub/unique_name/role -
  // see that class and Program.cs's TokenValidationParameters for why
  // "role", not the long ClaimTypes.Role URI). A single role decodes to a
  // string, more than one to an array - normalize to always be an array.
  const roleClaim = claims.role;
  const roles = Array.isArray(roleClaim) ? roleClaim : roleClaim ? [roleClaim] : [];

  return {
    id: claims.sub,
    userName: claims.unique_name,
    roles,
  };
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  // Starts true: on first load we don't yet know whether the stored refresh
  // token (if any) is still valid - ProtectedRoute waits on this instead of
  // bouncing straight to /login and then back once the silent refresh below
  // resolves.
  const [isInitializing, setIsInitializing] = useState(true);

  const applySession = useCallback((authResult) => {
    setSession({
      accessToken: authResult.access_token,
      accessTokenExpiresAt: authResult.access_token_expires_at,
      refreshToken: authResult.refresh_token,
    });
    setUser(userFromAccessToken(authResult.access_token));
  }, []);

  const clearAuthState = useCallback(() => {
    clearSession();
    setUser(null);
    // Force the next SignalR connection to rebuild with a fresh
    // accessTokenFactory read rather than keep whatever connection (if any)
    // was authenticated as the now-logged-out user.
    resetSignalR();
  }, []);

  // Silent refresh on startup: an access token never survives a page
  // reload (see tokenStorage.js), but the longer-lived refresh token does -
  // use it to restore the session without asking the user to log in again
  // every time they reload the page.
  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      const refreshToken = getRefreshToken();
      if (!refreshToken) {
        setIsInitializing(false);
        return;
      }

      try {
        const { data } = await apiClient.post('/auth/refresh', { refresh_token: refreshToken });
        if (!cancelled) {
          applySession(data);
        }
      } catch {
        if (!cancelled) {
          clearAuthState();
        }
      } finally {
        if (!cancelled) {
          setIsInitializing(false);
        }
      }
    }

    bootstrap();
    return () => {
      cancelled = true;
    };
  }, [applySession, clearAuthState]);

  // apiClient announces this when a request 401s and the refresh attempt
  // that follows also fails (or there was no refresh token to try) - the
  // session is gone regardless of what this component was doing.
  useEffect(() => onSessionExpired(clearAuthState), [clearAuthState]);

  const login = useCallback(
    async (userName, password) => {
      const result = await loginApi(userName, password);
      applySession(result);
    },
    [applySession]
  );

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken();
    try {
      if (refreshToken) {
        await logoutApi(refreshToken);
      }
    } catch {
      // Best-effort server-side revoke - clear the local session regardless,
      // matching AuthController.Logout's own "always succeeds" idempotence.
    } finally {
      clearAuthState();
    }
  }, [clearAuthState]);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: Boolean(user) && Boolean(getAccessToken()),
      isInitializing,
      login,
      logout,
    }),
    [user, isInitializing, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.');
  }
  return context;
}

/** True if the current user has any of the given roles - e.g. useHasRole('Admin', 'Tester'). */
export function useHasRole(...roles) {
  const { user } = useAuth();
  return Boolean(user) && roles.some((role) => user.roles.includes(role));
}
