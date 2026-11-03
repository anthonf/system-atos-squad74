import { createTheme } from '@mui/material/styles';

export const tokens = {
  light: {
    primary: '#bf152c',
    dark: '#7A2828',
    canvas: '#f9f9f9',
    muted: '#5f6b76',
  },
  dark: {
    primary: '#e85a72',
    dark: '#1a1a1a',
    canvas: '#0a0a0a',
    muted: '#9aa4ad',
  },
};

export function createAppTheme(dark: boolean) {
  return createTheme({
    palette: {
      mode: dark ? 'dark' : 'light',
      primary: { main: dark ? tokens.dark.primary : tokens.light.primary },
      background: {
        default: dark ? tokens.dark.canvas : tokens.light.canvas,
        paper: dark ? '#141414' : '#fff',
      },
    },
    typography: { fontFamily: 'Poppins, Arial, sans-serif' },
    shape: { borderRadius: 9 },
    components: {
      MuiCard: {
        styleOverrides: {
          root: {
            border: dark ? '1px solid #2a2a2a' : '1px solid #e5e7eb',
            boxShadow: dark ? 'none' : '0 1px 3px rgba(0,0,0,.08)',
          },
        },
      },
      MuiButton: { styleOverrides: { root: { minHeight: 40, textTransform: 'none' } } },
      MuiChip: { styleOverrides: { root: { fontWeight: 600 } } },
    },
  });
}
