// src/App.js
import React from 'react';
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard  from './pages/Dashboard';
import RunDetail  from './pages/RunDetail';
import FailedTests from './pages/FailedTests';
import ManualRun  from './pages/ManualRun';
import './App.css';

export default function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <aside className="sidebar">
          <div className="logo">
            <span className="logo-icon">🔒</span>
            <span>TestVault</span>
          </div>
          <nav>
            <NavLink to="/"            end>📊 Dashboard</NavLink>
            <NavLink to="/failed">     ❌ Failed Tests</NavLink>
            <NavLink to="/manual-run"> ▶️ Manual Run</NavLink>
          </nav>
        </aside>
        <main className="main-content">
          <Routes>
            <Route path="/"           element={<Dashboard />} />
            <Route path="/runs/:id"   element={<RunDetail />} />
            <Route path="/failed"     element={<FailedTests />} />
            <Route path="/manual-run" element={<ManualRun />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}
