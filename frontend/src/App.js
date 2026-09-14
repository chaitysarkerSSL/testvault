// src/App.js
import React from 'react';
import { BrowserRouter, Routes, Route, NavLink, Outlet } from 'react-router-dom';
import ProtectedRoute from './components/ProtectedRoute';
import { AuthProvider, useAuth } from './context/AuthContext';
import Dashboard from './pages/Dashboard';
import RunDetail from './pages/RunDetail';
import FailedTests from './pages/FailedTests';
import ManualRun from './pages/ManualRun';
import Login from './pages/Login';
import './App.css';

function SidebarFooter() {
  const { user, logout } = useAuth();
  if (!user) return null;

  return (
    <div className="sidebar-footer">
      <div className="sidebar-user">
        {user.userName}
        {user.roles.map((role) => (
          <span key={role} className="role-badge">
            {role}
          </span>
        ))}
      </div>
      <button className="sidebar-logout" onClick={logout}>
        Log out
      </button>
    </div>
  );
}

/**
 * Shared chrome (sidebar/nav) for every authenticated page, guarded by
 * ProtectedRoute and rendering the matched child route via <Outlet/> - the
 * standard React Router v6 "layout route" pattern, so App's route table
 * below stays a single flat list rather than nested <Routes>.
 */
function AppLayout() {
  return (
    <ProtectedRoute>
      <div className="app">
        <aside className="sidebar">
          <div className="logo">
            <span className="logo-icon">🔒</span>
            <span>TestVault</span>
          </div>
          <nav>
            <NavLink to="/" end>
              📊 Dashboard
            </NavLink>
            <NavLink to="/failed"> ❌ Failed Tests</NavLink>
            <NavLink to="/manual-run"> ▶️ Manual Run</NavLink>
          </nav>
          <SidebarFooter />
        </aside>
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </ProtectedRoute>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />

          <Route element={<AppLayout />}>
            <Route path="/" element={<Dashboard />} />
            <Route path="/runs/:id" element={<RunDetail />} />
            <Route path="/failed" element={<FailedTests />} />
            <Route path="/manual-run" element={<ManualRun />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
