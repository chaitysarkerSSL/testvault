// src/pages/Login.js
import React, { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Login() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  // Already logged in (e.g. navigated back to /login manually) - bounce
  // straight through rather than show the form again.
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      await login(userName, password);
      const redirectTo = location.state?.from?.pathname || '/';
      navigate(redirectTo, { replace: true });
    } catch (err) {
      // AuthController returns { success: false, message } on 401 - see
      // ExceptionHandlingMiddleware's AuthenticationException branch.
      setError(err.response?.data?.message || 'Login failed. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <div className="card login-card">
        <div className="logo login-logo">
          <span className="logo-icon">🔒</span>
          <span>TestVault</span>
        </div>

        <form onSubmit={handleSubmit}>
          <label className="field-label" htmlFor="userName">
            Username
          </label>
          <input
            id="userName"
            className="text-input"
            type="text"
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            autoComplete="username"
            autoFocus
            required
          />

          <label className="field-label" htmlFor="password">
            Password
          </label>
          <input
            id="password"
            className="text-input"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />

          {error && (
            <div className="alert alert-error" style={{ marginTop: 12, marginBottom: 12 }}>
              {error}
            </div>
          )}

          <button className="btn btn-primary login-submit" type="submit" disabled={submitting}>
            {submitting ? 'Signing in…' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  );
}
