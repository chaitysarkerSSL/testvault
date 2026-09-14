// src/components/ProtectedRoute.js
import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/**
 * Wraps a page element (matching App.js's existing element={<X/>} style,
 * rather than the nested-route/<Outlet/> pattern, so App.js needed minimal
 * restructuring). Redirects to /login if there's no session, remembering
 * where the user was headed so Login.js can send them back after
 * authenticating.
 */
export default function ProtectedRoute({ children }) {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    // Waiting on AuthContext's silent-refresh attempt (see AuthContext.js) -
    // showing nothing briefly beats a flash of the login page for a user
    // who's actually still logged in.
    return null;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
}
