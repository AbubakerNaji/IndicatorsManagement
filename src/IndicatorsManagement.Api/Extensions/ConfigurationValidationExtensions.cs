using System.Text;

namespace IndicatorsManagement.Api.Extensions;

/// <summary>
/// Fail-fast validation of the secrets and connection strings the API cannot start without.
/// No secret ships in <c>appsettings.json</c>; every environment supplies its own through
/// <c>appsettings.Development.json</c> (local only), user-secrets, or environment variables.
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>Minimum length for the HMAC-SHA256 signing key (256 bits).</summary>
    private const int MinimumJwtKeyLength = 32;

    /// <summary>
    /// Placeholder values that must never reach a deployed environment. Matching is
    /// case-insensitive and substring-based so variants of the shipped templates are caught.
    /// </summary>
    private static readonly string[] ForbiddenJwtKeyFragments =
    [
        "CHANGE-THIS",
        "local-development-only",
        "replace-me",
        "your-secret-key"
    ];

    /// <summary>
    /// Validates required configuration and throws a single, actionable
    /// <see cref="InvalidOperationException"/> listing everything that is missing.
    /// </summary>
    /// <exception cref="InvalidOperationException">One or more required settings are absent or unsafe.</exception>
    public static void ValidateRequiredConfiguration(this WebApplicationBuilder builder)
    {
        var errors = new List<string>();
        var isDevelopment = builder.Environment.IsDevelopment();

        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
            errors.Add("ConnectionStrings:DefaultConnection is not configured.");

        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("HangfireConnection")))
            errors.Add("ConnectionStrings:HangfireConnection is not configured.");

        var jwtKey = builder.Configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            errors.Add("Jwt:SecretKey is not configured.");
        }
        else
        {
            if (Encoding.UTF8.GetByteCount(jwtKey) < MinimumJwtKeyLength)
                errors.Add($"Jwt:SecretKey must be at least {MinimumJwtKeyLength} bytes long (HMAC-SHA256).");

            // A development placeholder outside Development would sign tokens anyone can forge.
            if (!isDevelopment && ForbiddenJwtKeyFragments.Any(f => jwtKey.Contains(f, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"Jwt:SecretKey is still a template placeholder and must be replaced in the '{builder.Environment.EnvironmentName}' environment.");
        }

        if (errors.Count == 0)
            return;

        throw new InvalidOperationException(BuildErrorMessage(builder.Environment.EnvironmentName, errors, isDevelopment));
    }

    private static string BuildErrorMessage(string environmentName, List<string> errors, bool isDevelopment)
    {
        var message = new StringBuilder()
            .AppendLine($"Startup configuration is incomplete for environment '{environmentName}':")
            .AppendLine();

        foreach (var error in errors)
            message.AppendLine($"  - {error}");

        message.AppendLine().AppendLine("How to fix:");

        if (isDevelopment)
        {
            message
                .AppendLine("  1. Start the local database:  docker compose -f docker-compose.dev.yml up -d")
                .AppendLine("  2. Create the settings file:  cp src/IndicatorsManagement.Api/appsettings.Development.json.example \\")
                .AppendLine("                                   src/IndicatorsManagement.Api/appsettings.Development.json")
                .AppendLine("  See Docs/09-development-setup.md.");
        }
        else
        {
            message
                .AppendLine("  Supply the values as environment variables (double underscore = nesting):")
                .AppendLine("    ConnectionStrings__DefaultConnection=...")
                .AppendLine("    ConnectionStrings__HangfireConnection=...")
                .AppendLine("    Jwt__SecretKey=$(openssl rand -base64 48)")
                .AppendLine("  See Docs/10-deployment.md.");
        }

        return message.ToString();
    }
}
