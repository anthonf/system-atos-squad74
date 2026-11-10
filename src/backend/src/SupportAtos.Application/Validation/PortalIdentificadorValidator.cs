using System.Text.RegularExpressions;
using SupportAtos.Application.Exceptions;

namespace SupportAtos.Application.Validation;

public static partial class PortalIdentificadorValidator
{
    public const int TamanhoMaximo = 100;

    public static string Validar(string? valor, string campo)
    {
        var limpo = valor?.Trim();

        if (string.IsNullOrWhiteSpace(limpo))
        {
            throw new ValidationException($"O campo '{campo}' é obrigatório.");
        }

        if (limpo.Length > TamanhoMaximo)
        {
            throw new ValidationException($"O campo '{campo}' deve ter no máximo {TamanhoMaximo} caracteres.");
        }

        if (!Padrao().IsMatch(limpo))
        {
            throw new ValidationException(
                $"O campo '{campo}' contém caracteres inválidos. Use apenas letras, números, ponto, underscore, dois-pontos e hífen.");
        }

        return limpo;
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Padrao();
}
