using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportAtos.Application.DTOs;

public class AbrirChamadoPortalInput
{
    public PortalUsuarioInput? Usuario { get; set; }

    public PortalGrupoInput? GrupoEmpresa { get; set; }

    public string? Produto { get; set; }

    public string? Categoria { get; set; }

    public string? Titulo { get; set; }

    public string? Descricao { get; set; }

    public string? MensagemInicial { get; set; }

    public List<AnexoInput>? Anexos { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CamposExtras { get; set; }
}

public class PortalUsuarioInput
{
    [JsonConverter(typeof(StringOuNumeroConverter))]
    public string? Id { get; set; }

    public string? Nome { get; set; }

    public string? Email { get; set; }
}

public class PortalGrupoInput
{
    [JsonConverter(typeof(StringOuNumeroConverter))]
    public string? Id { get; set; }

    public string? Nome { get; set; }
}

public class AnexoInput
{
    public string? NomeArquivo { get; set; }

    public string? Url { get; set; }

    public int TamanhoBytes { get; set; }
}

public sealed class StringOuNumeroConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException("O identificador deve ser um número ou uma string.")
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
