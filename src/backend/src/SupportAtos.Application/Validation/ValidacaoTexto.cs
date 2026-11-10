using System.Net.Mail;
using SupportAtos.Application.Exceptions;

namespace SupportAtos.Application.Validation;

public static class ValidacaoTexto
{
    public static string Obrigatorio(string? valor, string campo, int tamanhoMaximo)
    {
        var limpo = valor?.Trim();

        if (string.IsNullOrWhiteSpace(limpo))
        {
            throw new ValidationException($"O campo '{campo}' é obrigatório.");
        }

        if (limpo.Length > tamanhoMaximo)
        {
            throw new ValidationException($"O campo '{campo}' deve ter no máximo {tamanhoMaximo} caracteres.");
        }

        return limpo;
    }

    public static string? Opcional(string? valor, string campo, int tamanhoMaximo)
    {
        var limpo = valor?.Trim();
        if (string.IsNullOrWhiteSpace(limpo))
        {
            return null;
        }

        if (limpo.Length > tamanhoMaximo)
        {
            throw new ValidationException($"O campo '{campo}' deve ter no máximo {tamanhoMaximo} caracteres.");
        }

        return limpo;
    }

    public static string Email(string? valor)
    {
        var limpo = Obrigatorio(valor, "email", 255);

        if (!MailAddress.TryCreate(limpo, out var endereco)
            || !string.Equals(endereco.Address, limpo, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("O campo 'email' não contém um e-mail válido.");
        }

        return limpo;
    }
}
