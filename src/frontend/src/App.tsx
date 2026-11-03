import { useEffect, useState } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { Toaster } from 'sonner';
import { AuthProvider } from './auth/AuthContext';
import { RequireAuth } from './auth/RequireAuth';
import { Layout } from './components/Layout';
import { Login } from './pages/Login';
import { Fila } from './pages/Fila';
import { DetalheTicket } from './pages/DetalheTicket';
import { Dashboard } from './pages/Dashboard';
import { createAppTheme } from './theme/theme';

export default function App() {
  const [dark, setDark] = useState(() => localStorage.getItem('supportatos-theme') === 'dark');
  useEffect(() => {
    document.body.classList.toggle('dark', dark);
    document.body.classList.toggle('light', !dark);
    localStorage.setItem('supportatos-theme', dark ? 'dark' : 'light');
  }, [dark]);

  const theme = createAppTheme(dark);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Toaster position="top-right" richColors />
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route
              path="/"
              element={
                <RequireAuth>
                  <Layout dark={dark} onToggleDark={() => setDark((d) => !d)}>
                    <Dashboard />
                  </Layout>
                </RequireAuth>
              }
            />
            <Route
              path="/fila"
              element={
                <RequireAuth>
                  <Layout dark={dark} onToggleDark={() => setDark((d) => !d)}>
                    <Fila />
                  </Layout>
                </RequireAuth>
              }
            />
            <Route
              path="/chamados/:codigo"
              element={
                <RequireAuth>
                  <Layout dark={dark} onToggleDark={() => setDark((d) => !d)}>
                    <DetalheTicket />
                  </Layout>
                </RequireAuth>
              }
            />
            <Route path="*" element={<Navigate to="/fila" replace />} />
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  );
}
