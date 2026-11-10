// Internacionalização (pt / en / es) com i18next + react-i18next.
// O idioma escolhido fica em localStorage ("atos_lang"); se não houver, usa o do navegador e cai para pt.
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import 'dayjs/locale/en';
import 'dayjs/locale/es';
import pt from './locales/pt';
import en from './locales/en';
import es from './locales/es';

export const LANGS = ['pt', 'en', 'es'] as const;
export type Lang = (typeof LANGS)[number];
export const LANG_NAMES: Record<Lang, string> = { pt: 'Português', en: 'English', es: 'Español' };
export const DAYJS_LOCALE: Record<Lang, string> = { pt: 'pt-br', en: 'en', es: 'es' };

const STORAGE_KEY = 'atos_lang';
const isLang = (v: unknown): v is Lang => LANGS.includes(v as Lang);

function initialLanguage(): Lang {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (isLang(stored)) return stored;
  } catch { /* localStorage indisponível */ }
  const browser = navigator.language.slice(0, 2);
  return isLang(browser) ? browser : 'pt';
}

function applyLanguage(lang: string) {
  const l: Lang = isLang(lang) ? lang : 'pt';
  dayjs.locale(DAYJS_LOCALE[l]);
  document.documentElement.lang = l === 'pt' ? 'pt-BR' : l;
  try { window.localStorage.setItem(STORAGE_KEY, l); } catch { /* ignora */ }
}

void i18n.use(initReactI18next).init({
  resources: { pt: { translation: pt }, en: { translation: en }, es: { translation: es } },
  lng: initialLanguage(),
  fallbackLng: 'pt',
  interpolation: { escapeValue: false }, // o React já escapa
});

applyLanguage(i18n.language);
i18n.on('languageChanged', applyLanguage);

export default i18n;
