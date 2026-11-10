// Datas: dayjs, sempre exibidas como DD/MM/YYYY (com HH:mm quando houver horário), em qualquer idioma.
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';

export const DATE_FORMAT = 'DD/MM/YYYY';
export const DATETIME_FORMAT = 'DD/MM/YYYY HH:mm';

export { dayjs };
export type { Dayjs };

/** A API devolve datas UTC sem sufixo "Z"; força UTC antes de converter para o horário local. */
export function parseApiDate(iso: string): Dayjs {
  return dayjs(/[zZ]$|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`);
}

export const formatDate = (value: string | Date | Dayjs): string => dayjs(value).format(DATE_FORMAT);
export const formatDateTime = (value: string | Date | Dayjs): string => dayjs(value).format(DATETIME_FORMAT);
