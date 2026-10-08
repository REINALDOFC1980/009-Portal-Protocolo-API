using System.Text.Json.Serialization;

namespace Portal.Protocolos.API.Models.Seguranca.GovBr;

public sealed class GovBrUserInfo
{
    [JsonPropertyName("sub")]
    public string Cpf { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("social_name")]
    public string? NomeSocial { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerificado { get; set; }

    [JsonPropertyName("phone_number")]
    public string? Telefone { get; set; }

    [JsonPropertyName("phone_number_verified")]
    public bool TelefoneVerificado { get; set; }

    [JsonPropertyName("picture")]
    public string? Foto { get; set; }
}
