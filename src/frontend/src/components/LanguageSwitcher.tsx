// Seletor de idioma (PT / EN / ES). `onDark` ajusta as cores para o menu lateral (fundo vinho).
import React from 'react';
import { ToggleButton, ToggleButtonGroup } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { LANGS, LANG_NAMES } from '../i18n';
import type { Lang } from '../i18n';

export default function LanguageSwitcher({ onDark = false }: { onDark?: boolean }) {
  const { t, i18n } = useTranslation();
  const current = (LANGS as readonly string[]).includes(i18n.language) ? (i18n.language as Lang) : 'pt';

  const sx = onDark
    ? {
      color: 'rgba(255,255,255,0.8)', borderColor: 'rgba(255,255,255,0.25)',
      '&.Mui-selected, &.Mui-selected:hover': { color: '#fff', bgcolor: 'rgba(255,255,255,0.18)' },
      '&:hover': { bgcolor: 'rgba(255,255,255,0.08)' },
    }
    : {};

  return (
    <ToggleButtonGroup
      exclusive size="small" value={current} aria-label={t('common.language')}
      onChange={(_, value: Lang | null) => { if (value) void i18n.changeLanguage(value); }}
      sx={{ '& .MuiToggleButton-root': { px: 1.25, py: 0.25, fontSize: 11, fontWeight: 700, minHeight: 28, ...sx } }}
    >
      {LANGS.map((l) => (
        <ToggleButton key={l} value={l} aria-label={LANG_NAMES[l]} title={LANG_NAMES[l]}>{l.toUpperCase()}</ToggleButton>
      ))}
    </ToggleButtonGroup>
  );
}
