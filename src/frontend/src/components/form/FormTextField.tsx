// Campo de texto/seleção ligado ao react-hook-form. O erro vem do Yup e aparece em <LineErrorForm/> sob o campo.
// Para campos obrigatórios passe `required`: o asterisco vermelho fica no rótulo desde o início
// (a validação não é o único aviso de obrigatoriedade).
import React from 'react';
import { Box, TextField } from '@mui/material';
import type { TextFieldProps } from '@mui/material';
import { Controller } from 'react-hook-form';
import type { Control, FieldValues, Path } from 'react-hook-form';
import LineErrorForm from './LineErrorForm';

type BaseProps = Omit<TextFieldProps, 'name' | 'value' | 'defaultValue' | 'onChange' | 'onBlur' | 'error' | 'helperText' | 'inputRef'>;

type FormTextFieldProps<T extends FieldValues> = BaseProps & {
  name: Path<T>;
  control: Control<T>;
  /** Variáveis de interpolação da mensagem de erro (ex.: { max: 200 }). */
  errorParams?: Record<string, unknown>;
  /** Chamado depois que o valor do formulário é atualizado (útil para campos dependentes). */
  onValueChange?: (value: string) => void;
};

export default function FormTextField<T extends FieldValues>({
  name, control, errorParams, onValueChange, ...textFieldProps
}: FormTextFieldProps<T>) {
  const errorId = `${String(name)}-error`;
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <Box>
          <TextField
            {...textFieldProps}
            name={field.name}
            value={field.value ?? ''}
            onChange={(e) => { field.onChange(e); onValueChange?.(e.target.value); }}
            onBlur={field.onBlur}
            inputRef={field.ref}
            error={Boolean(fieldState.error)}
            inputProps={{ ...textFieldProps.inputProps, 'aria-describedby': fieldState.error ? errorId : undefined }}
          />
          <LineErrorForm id={errorId} error={fieldState.error} params={errorParams} />
        </Box>
      )}
    />
  );
}
