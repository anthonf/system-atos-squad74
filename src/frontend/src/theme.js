// Tema MUI da Atos Capital. Fonte única dos tokens do design system (claro e escuro).
import { createTheme } from '@mui/material/styles';

export const TOKENS = {
  // Paleta institucional: VINHO. Vermelho puro fica reservado para erro / alerta / ações destrutivas (fechar, excluir).
  // Contrastes conferidos (WCAG): texto >= 4.5:1, bordas de campos e ícones >= 3:1, nos dois temas.
  //   brandPrimary     = preenchimento de botões e destaques (texto branco por cima)
  //   brandPrimaryEnd  = fim do degradê do botão
  //   brandPrimaryHover= botão em hover
  //   brandText        = cor de vinho usada como TEXTO/ÍCONE sobre o fundo da tela (links, abas, eyebrow, gráficos)
  //   brandSoft        = fundo suave em vinho (ícones, hover, cursor de gráfico)
  light: {
    brandPrimary: '#7a1f3f', brandPrimaryEnd: '#6b1a37', brandPrimaryHover: '#5c1530',
    brandText: '#7a1f3f', brandSoft: 'rgba(122,31,63,0.10)',
    brandPrimaryStrong: '#6b1b35', brandPrimaryMuted: '#4a1228',
    brandPrimaryShadow: 'rgba(122,31,63,0.28)', onBrand: '#ffffff',
    surfaceCanvas: '#f7f5f6', surfaceSoft: '#fbf9f9', surfaceBase: '#ffffff', surfaceAlt: '#faf8f9', surfaceOverlay: 'rgba(0,0,0,0.45)',
    borderSubtle: '#e6e1e3', borderDefault: '#d6d1d3', borderStrong: '#8f8f8f',
    textPrimary: '#212121', textSecondary: '#555555', textMuted: '#6b6b6b', textInverse: '#ffffff',
    success: '#1b6b2f', successBg: 'rgba(27,107,47,0.14)',
    warning: '#9a4a00', warningBg: 'rgba(245,158,11,0.18)',
    error: '#c62828', errorBg: 'rgba(198,40,40,0.12)',
    info: '#1565c0', infoBg: 'rgba(21,101,192,0.12)',
    purple: '#5e35b1', purpleBg: 'rgba(94,53,177,0.12)',
    menuGradient: 'linear-gradient(0deg, #4a1228, #6b1b35)', menuText: '#ffffff',
    tableHeaderBg: '#efeaec', tableRowAlt: '#faf8f9', scrollbarThumb: '#c9c2c5',
  },
  dark: {
    brandPrimary: '#b03a63', brandPrimaryEnd: '#9b2c55', brandPrimaryHover: '#c24673',
    brandText: '#ec9bb2', brandSoft: 'rgba(236,155,178,0.14)',
    brandPrimaryStrong: '#6b1b35', brandPrimaryMuted: '#4a1228',
    brandPrimaryShadow: 'rgba(176,58,99,0.40)', onBrand: '#ffffff',
    surfaceCanvas: '#0f0c0d', surfaceSoft: '#141011', surfaceBase: '#1a1a1a', surfaceAlt: '#232021', surfaceOverlay: 'rgba(0,0,0,0.65)',
    borderSubtle: '#2e2a2b', borderDefault: '#4a4546', borderStrong: '#7a7577',
    textPrimary: '#f5f5f5', textSecondary: '#cfcfcf', textMuted: '#a3a3a3', textInverse: '#212121',
    success: '#66bb6a', successBg: 'rgba(102,187,106,0.20)',
    warning: '#ffa726', warningBg: 'rgba(255,167,38,0.20)',
    error: '#ff6e6b', errorBg: 'rgba(255,110,107,0.20)',
    info: '#42a5f5', infoBg: 'rgba(66,165,245,0.20)',
    purple: '#b39ddb', purpleBg: 'rgba(179,157,219,0.20)',
    menuGradient: 'linear-gradient(0deg, #2b0d19, #3d1223)', menuText: '#f5f5f5',
    tableHeaderBg: '#262223', tableRowAlt: '#1f1c1d', scrollbarThumb: '#4a4546',
  },
};

export const FONT = '"IBM Plex Sans", system-ui, sans-serif';
export const MONO = '"IBM Plex Mono", ui-monospace, monospace';

export function buildTheme(mode) {
  const t = TOKENS[mode];
  return createTheme({
    palette: {
      mode,
      // primary.main = vinho legível como texto/ícone no tema atual (claro: vinho escuro; escuro: vinho claro).
      // O preenchimento dos botões vem de brandPrimary (ver MuiButton.containedPrimary).
      primary: { main: t.brandText, dark: t.brandPrimaryHover, contrastText: mode === 'light' ? t.onBrand : '#2b0d19' },
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
          // Foco por teclado sempre visível, nos dois temas.
          '*:focus-visible': { outline: `2px solid ${t.brandText}`, outlineOffset: 2 },
          '.MuiOutlinedInput-input:focus-visible': { outline: 'none' },
          '::selection': { background: t.brandSoft },
        },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
        styleOverrides: {
          root: { minHeight: 40, paddingInline: 16 },
          containedPrimary: {
            color: t.onBrand,
            background: `linear-gradient(100deg, ${t.brandPrimary}, ${t.brandPrimaryEnd})`,
            boxShadow: `0 6px 18px ${t.brandPrimaryShadow}`,
            transition: 'transform .16s ease, box-shadow .16s ease, background .16s ease',
            '&:hover': { background: t.brandPrimaryHover, transform: 'translateY(-1px)', boxShadow: `0 10px 24px ${t.brandPrimaryShadow}` },
            '&.Mui-disabled': { opacity: 0.7, color: t.onBrand },
          },
          outlined: {
            borderColor: t.borderStrong, color: t.textPrimary,
            '&:hover': { borderColor: t.brandText, background: t.brandSoft },
          },
          text: { '&:hover': { background: t.brandSoft } },
        },
      },
      MuiIconButton: { styleOverrides: { root: { '&:hover': { color: t.brandText, background: t.brandSoft } } } },
      MuiPaper: { styleOverrides: { root: { backgroundImage: 'none' }, outlined: { borderColor: t.borderSubtle } } },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            background: t.surfaceAlt, fontSize: 13,
            '& fieldset': { borderColor: t.borderStrong },
            '&:hover fieldset': { borderColor: `${t.textSecondary} !important` },
            '&.Mui-focused fieldset': { borderColor: `${t.brandText} !important`, borderWidth: 2 },
            '&.Mui-focused': { boxShadow: `0 0 0 3px ${t.brandSoft}` },
          },
        },
      },
      MuiInputLabel: { styleOverrides: { root: { fontSize: 13 } } },
      MuiChip: { styleOverrides: { root: { fontWeight: 600, fontSize: 12 }, outlined: { borderColor: t.borderStrong } } },
      MuiTableCell: { styleOverrides: { root: { borderColor: t.borderSubtle, fontSize: 13 } } },
      MuiTab: { styleOverrides: { root: { minHeight: 40, fontSize: 13 } } },
      MuiTabs: { styleOverrides: { indicator: { height: 2 } } },
      MuiTooltip: { defaultProps: { arrow: true } },
      MuiDialog: { styleOverrides: { paper: { borderRadius: 14 } } },
      MuiBackdrop: { styleOverrides: { root: { backgroundColor: t.surfaceOverlay } } },
    },
  });
}
