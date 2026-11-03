// Tema MUI da Atos Capital. Fonte única dos tokens do design system (claro e escuro).
import { createTheme } from '@mui/material/styles';

export const TOKENS = {
  light: {
    brandPrimary: '#bf152c', brandPrimaryHover: '#a01024', brandPrimaryStrong: '#893939', brandPrimaryMuted: '#7A2828',
    brandPrimaryShadow: 'rgba(191,21,44,0.28)', onBrand: '#ffffff',
    surfaceCanvas: '#f9f9f9', surfaceSoft: '#fbf9f9', surfaceBase: '#ffffff', surfaceAlt: '#fafafa', surfaceOverlay: 'rgba(0,0,0,0.45)',
    borderSubtle: '#ececec', borderDefault: '#e0e0e0', borderStrong: '#bdbdbd',
    textPrimary: '#212121', textSecondary: '#555555', textMuted: '#939393', textInverse: '#ffffff',
    success: '#2e7d32', successBg: 'rgba(21,163,9,0.20)',
    warning: '#ff9800', warningBg: 'rgba(255,152,0,0.20)',
    error: '#d32f2f', errorBg: 'rgba(211,47,47,0.20)',
    info: '#1976d2', infoBg: 'rgba(33,150,243,0.20)',
    purple: '#7b1fa2', purpleBg: 'rgba(123,31,162,0.16)',
    menuGradient: 'linear-gradient(0deg, #7A2828, #893939)', menuText: '#ffffff',
    tableHeaderBg: '#ECEFF1', tableRowAlt: '#fafafa', scrollbarThumb: '#e5e5e5',
  },
  dark: {
    brandPrimary: '#571721', brandPrimaryHover: '#5e282f', brandPrimaryStrong: '#681e29', brandPrimaryMuted: '#e7dddf',
    brandPrimaryShadow: 'rgba(232,90,114,0.35)', onBrand: '#ffffff',
    surfaceCanvas: '#0a0a0a', surfaceSoft: '#0f0f0f', surfaceBase: '#1a1a1a', surfaceAlt: '#212121', surfaceOverlay: 'rgba(0,0,0,0.65)',
    borderSubtle: '#252525', borderDefault: '#2e2e2e', borderStrong: '#3d3d3d',
    textPrimary: '#f5f5f5', textSecondary: '#c4c4c4', textMuted: '#8a8a8a', textInverse: '#212121',
    success: '#66bb6a', successBg: 'rgba(21,163,9,0.20)',
    warning: '#ffa726', warningBg: 'rgba(255,152,0,0.20)',
    error: '#ef5350', errorBg: 'rgba(211,47,47,0.20)',
    info: '#42a5f5', infoBg: 'rgba(33,150,243,0.20)',
    purple: '#ba68c8', purpleBg: 'rgba(123,31,162,0.16)',
    menuGradient: 'linear-gradient(0deg, #2a0e0e, #3a1818)', menuText: '#f5f5f5',
    tableHeaderBg: '#212121', tableRowAlt: '#1f1f1f', scrollbarThumb: '#3a3a3a',
  },
};

export const FONT = '"IBM Plex Sans", system-ui, sans-serif';
export const MONO = '"IBM Plex Mono", ui-monospace, monospace';

export function buildTheme(mode) {
  const t = TOKENS[mode];
  return createTheme({
    palette: {
      mode,
      primary: { main: t.brandPrimary, dark: t.brandPrimaryHover, contrastText: t.onBrand },
      secondary: { main: t.brandPrimaryStrong },
      error: { main: t.error },
      warning: { main: t.warning },
      info: { main: t.info },
      success: { main: t.success },
      background: { default: t.surfaceCanvas, paper: t.surfaceBase },
      text: { primary: t.textPrimary, secondary: t.textSecondary, disabled: t.textMuted },
      divider: t.borderSubtle,
    },
    shape: { borderRadius: 8 },
    typography: {
      fontFamily: FONT,
      h1: { fontSize: 24, fontWeight: 600, letterSpacing: '-0.5px' },
      h2: { fontSize: 16, fontWeight: 600 },
      h3: { fontSize: 14, fontWeight: 700 },
      body1: { fontSize: 14 },
      body2: { fontSize: 13 },
      caption: { fontSize: 11, color: t.textMuted },
      overline: { fontSize: 11, fontWeight: 700, letterSpacing: '0.14em', lineHeight: 1.6 },
      button: { textTransform: 'none', fontWeight: 600, fontSize: 13 },
    },
    // Tokens extras do design system que não existem na paleta do MUI.
    atos: {
      ...t,
      tones: {
        info: { fg: t.info, bg: t.infoBg },
        warning: { fg: t.warning, bg: t.warningBg },
        error: { fg: t.error, bg: t.errorBg },
        success: { fg: t.success, bg: t.successBg },
        purple: { fg: t.purple, bg: t.purpleBg },
        muted: { fg: t.textMuted, bg: t.borderSubtle },
      },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          '*::-webkit-scrollbar': { width: 10, height: 10 },
          '*::-webkit-scrollbar-thumb': { background: t.scrollbarThumb, borderRadius: 8 },
          '*::-webkit-scrollbar-track': { background: 'transparent' },
        },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: { minHeight: 40, paddingInline: 16 },
          containedPrimary: {
            background: `linear-gradient(100deg, ${t.brandPrimary}, ${t.brandPrimaryHover})`,
            boxShadow: `0 8px 24px ${t.brandPrimaryShadow}`,
            transition: 'transform .16s ease, box-shadow .16s ease',
            '&:hover': { transform: 'translateY(-1px)', boxShadow: `0 12px 28px ${t.brandPrimaryShadow}` },
            '&.Mui-disabled': { opacity: 0.7, color: t.onBrand },
          },
          outlined: { borderColor: t.borderDefault, color: t.textSecondary, '&:hover': { borderColor: t.borderStrong, background: 'transparent' } },
        },
      },
      MuiIconButton: { styleOverrides: { root: { '&:hover': { color: t.brandPrimary } } } },
      MuiPaper: { styleOverrides: { root: { backgroundImage: 'none' }, outlined: { borderColor: t.borderSubtle } } },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            background: t.surfaceAlt, fontSize: 13,
            '& fieldset': { borderColor: t.borderDefault },
            '&:hover fieldset': { borderColor: `${t.borderStrong} !important` },
            '&.Mui-focused fieldset': { borderColor: `${t.brandPrimary} !important`, borderWidth: 1 },
            '&.Mui-focused': { boxShadow: `0 0 0 3px ${t.brandPrimaryShadow}` },
          },
        },
      },
      MuiInputLabel: { styleOverrides: { root: { fontSize: 13 } } },
      MuiChip: { styleOverrides: { root: { fontWeight: 600, fontSize: 12 } } },
      MuiTableCell: { styleOverrides: { root: { borderColor: t.borderSubtle, fontSize: 13 } } },
      MuiTab: { styleOverrides: { root: { minHeight: 40, fontSize: 13 } } },
      MuiTabs: { styleOverrides: { indicator: { height: 2 } } },
      MuiTooltip: { defaultProps: { arrow: true } },
      MuiDialog: { styleOverrides: { paper: { borderRadius: 14 } } },
      MuiBackdrop: { styleOverrides: { root: { backgroundColor: t.surfaceOverlay } } },
    },
  });
}
