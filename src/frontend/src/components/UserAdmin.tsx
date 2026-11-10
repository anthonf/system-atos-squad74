// Administração: cadastro de usuários (POST /api/usuarios) e lista dos usuários do grupo selecionado.
// Tudo vem da API real; exige token com perfil Admin. Formulário: react-hook-form + Yup (erros em LineErrorForm).
import React, { useCallback, useEffect, useState } from 'react';
import {
  Box, Button, Chip, CircularProgress, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableHead,
  TableRow, Typography,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { toast } from 'sonner';
import { PersonAddAlt1, Refresh } from '@mui/icons-material';
import { ApiError, cadastrarUsuario, listarGruposEmpresa, listarUsuariosDoGrupo } from '../api';
import type { GrupoEmpresaDto, PapelApi, UsuarioDto } from '../api';
import i18n from '../i18n';
import { notifyApiError } from '../notify';
import FormTextField from './form/FormTextField';
import { Eyebrow } from './ui';

const PAPEIS: PapelApi[] = ['Cliente', 'Agente', 'Supervisor', 'Admin'];

const userSchema = yup.object({
  grupoId: yup.string().required('validation.required'),
  nome: yup.string().trim().required('validation.required'),
  email: yup.string().trim().required('validation.required').email('validation.email'),
  papel: yup.string().required('validation.required'),
});
type UserForm = yup.InferType<typeof userSchema>;

export default function UserAdmin() {
  const { atos } = useTheme();
  const { t } = useTranslation();

  const [grupos, setGrupos] = useState<GrupoEmpresaDto[]>([]);
  const [loadingGrupos, setLoadingGrupos] = useState(true);
  const [gruposError, setGruposError] = useState(false);

  const [usuarios, setUsuarios] = useState<UsuarioDto[]>([]);
  const [loadingUsuarios, setLoadingUsuarios] = useState(false);

  const { control, handleSubmit, reset, setError, setValue, watch, formState: { isSubmitting } } = useForm<UserForm>({
    resolver: yupResolver(userSchema),
    mode: 'onTouched',
    defaultValues: { grupoId: '', nome: '', email: '', papel: 'Cliente' },
  });
  const grupoId = watch('grupoId');
  const papel = watch('papel') as PapelApi;

  const carregarGrupos = useCallback(() => {
    setLoadingGrupos(true);
    setGruposError(false);
    listarGruposEmpresa()
      .then((lista) => {
        setGrupos(lista);
        if (lista.length > 0) setValue('grupoId', String(lista[0].id));
      })
      .catch((e: unknown) => { setGruposError(true); notifyApiError(e, i18n.t('users.loadGroupsError')); })
      .finally(() => setLoadingGrupos(false));
  }, [setValue]);

  const carregarUsuarios = useCallback((id: number) => {
    setLoadingUsuarios(true);
    listarUsuariosDoGrupo(id)
      .then(setUsuarios)
      .catch((e: unknown) => { setUsuarios([]); notifyApiError(e, i18n.t('users.loadUsersError')); })
      .finally(() => setLoadingUsuarios(false));
  }, []);

  useEffect(carregarGrupos, [carregarGrupos]);
  useEffect(() => {
    if (grupoId) carregarUsuarios(Number(grupoId));
    else setUsuarios([]);
  }, [grupoId, carregarUsuarios]);

  const onSubmit = async (data: UserForm) => {
    try {
      const criado = await cadastrarUsuario({ nome: data.nome, email: data.email, grupoEmpresaId: Number(data.grupoId), papel: data.papel as PapelApi });
      toast.success(t('users.created', { name: criado.nome }));
      reset({ grupoId: data.grupoId, nome: '', email: '', papel: 'Cliente' });
      carregarUsuarios(Number(data.grupoId));
    } catch (err) {
      // 409: e-mail já cadastrado — mostra no próprio campo.
      if (err instanceof ApiError && err.status === 409) setError('email', { type: 'server', message: 'users.emailExists' });
      else notifyApiError(err, t('users.createError'));
    }
  };

  const grupoAtual = grupos.find((g) => String(g.id) === grupoId);

  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Box sx={{ mb: 3.5 }}>
        <Eyebrow>{t('users.eyebrow')}</Eyebrow>
        <Typography variant="h1">{t('users.title')}</Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>{t('users.subtitle')}</Typography>
      </Box>

      {gruposError ? (
        <Stack spacing={2} alignItems="flex-start">
          <Typography color="text.secondary">{t('users.loadGroupsError')}</Typography>
          <Button variant="outlined" startIcon={<Refresh />} onClick={carregarGrupos}>{t('common.tryAgain')}</Button>
        </Stack>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'minmax(320px, 400px) 1fr' }, gap: 3, alignItems: 'start' }}>
          <Paper variant="outlined" component="form" onSubmit={handleSubmit(onSubmit)} noValidate sx={{ p: 2.5 }}>
            <Typography variant="h2" sx={{ mb: 2 }}>{t('users.newUser')}</Typography>
            {loadingGrupos ? (
              <Stack alignItems="center" spacing={1.5} sx={{ py: 5 }}>
                <CircularProgress size={24} />
                <Typography variant="body2" color="text.secondary">{t('users.loadingGroups')}</Typography>
              </Stack>
            ) : (
              <Stack spacing={2}>
                <FormTextField<UserForm> name="grupoId" control={control} select required fullWidth size="small" label={t('users.group')}>
                  {grupos.map((g) => <MenuItem key={g.id} value={String(g.id)}>{g.nome}</MenuItem>)}
                </FormTextField>
                <FormTextField<UserForm> name="nome" control={control} required fullWidth size="small" autoComplete="off" label={t('users.name')} />
                <FormTextField<UserForm> name="email" control={control} required fullWidth size="small" type="email" autoComplete="off"
                  label={t('users.email')} placeholder={t('users.emailPlaceholder')} />
                <FormTextField<UserForm> name="papel" control={control} select required fullWidth size="small" label={t('users.role')}>
                  {PAPEIS.map((p) => <MenuItem key={p} value={p}>{t(`users.roles.${p}`)}</MenuItem>)}
                </FormTextField>
                {PAPEIS.includes(papel) && <Typography variant="caption" sx={{ mt: '-8px !important' }}>{t(`users.roleHints.${papel}`)}</Typography>}
                <Button type="submit" variant="contained" disabled={isSubmitting || grupos.length === 0}
                  startIcon={isSubmitting ? <CircularProgress size={14} color="inherit" /> : <PersonAddAlt1 />}>
                  {isSubmitting ? t('users.submitting') : t('users.submit')}
                </Button>
                {grupos.length === 0 && <Typography variant="body2" color="text.secondary">{t('users.noGroups')}</Typography>}
              </Stack>
            )}
          </Paper>

          <Paper variant="outlined" sx={{ p: 2.5, minWidth: 0 }}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
              <Box>
                <Typography variant="h2">{t('users.groupUsers')}</Typography>
                <Typography variant="caption">{grupoAtual ? grupoAtual.nome : t('users.selectGroup')}</Typography>
              </Box>
              <Button size="small" variant="outlined" startIcon={<Refresh />} disabled={!grupoId || loadingUsuarios}
                onClick={() => grupoId && carregarUsuarios(Number(grupoId))}>
                {t('common.refresh')}
              </Button>
            </Stack>

            {loadingUsuarios ? (
              <Stack alignItems="center" sx={{ py: 5 }}><CircularProgress size={24} /></Stack>
            ) : usuarios.length === 0 ? (
              <Typography sx={{ py: 5, textAlign: 'center', color: 'text.disabled' }}>{t('users.none')}</Typography>
            ) : (
              <Box sx={{ overflowX: 'auto' }}>
                <Table size="small" aria-label={t('users.groupUsers')}>
                  <TableHead>
                    <TableRow sx={{ bgcolor: atos.tableHeaderBg }}>
                      <TableCell>{t('users.table.name')}</TableCell>
                      <TableCell>{t('users.table.email')}</TableCell>
                      <TableCell>{t('users.table.role')}</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {usuarios.map((u) => (
                      <TableRow key={u.id}>
                        <TableCell sx={{ fontWeight: 600 }}>{u.nome}</TableCell>
                        <TableCell>{u.email}</TableCell>
                        <TableCell><Chip size="small" variant="outlined" label={t(`users.roles.${u.papel}`, u.papel)} /></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Box>
            )}
          </Paper>
        </Box>
      )}
    </Box>
  );
}
