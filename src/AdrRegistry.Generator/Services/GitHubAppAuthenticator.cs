using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace AdrRegistry.Generator.Services;

/// <summary>
/// Handles GitHub App authentication including JWT generation and installation token exchange.
/// </summary>
public class GitHubAppAuthenticator
{
    private readonly int _appId;
    private readonly string _privateKey;

    public GitHubAppAuthenticator(int appId, string privateKeyPath)
    {
        _appId = appId;

        if (!File.Exists(privateKeyPath))
            throw new FileNotFoundException($"GitHub App private key not found at: {privateKeyPath}");

        _privateKey = File.ReadAllText(privateKeyPath);
    }

    public GitHubAppAuthenticator(int appId, string privateKey, bool isKeyContent)
    {
        _appId = appId;
        _privateKey = privateKey;
    }

    /// <summary>
    /// Generates a JWT for authenticating as the GitHub App.
    /// </summary>
    public string GenerateJwt()
    {
        var now = DateTimeOffset.UtcNow;
        var handler = new JsonWebTokenHandler();

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _appId.ToString(),
            IssuedAt = now.AddSeconds(-60).DateTime,
            Expires = now.AddMinutes(10).DateTime,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256)
        };

        return handler.CreateToken(tokenDescriptor);
    }

    /// <summary>
    /// Gets an installation access token for the GitHub App.
    /// </summary>
    /// <param name="installationId">The installation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The installation access token.</returns>
    public async Task<string> GetInstallationTokenAsync(long installationId, CancellationToken ct = default)
    {
        var jwt = GenerateJwt();

        var appClient = new GitHubClient(new ProductHeaderValue("AdrRegistry"))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };

        var token = await appClient.GitHubApps.CreateInstallationToken(installationId);
        return token.Token;
    }

    /// <summary>
    /// Creates a GitHubClient authenticated with an installation token.
    /// </summary>
    /// <param name="installationId">The installation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An authenticated GitHubClient.</returns>
    public async Task<GitHubClient> CreateClientAsync(long installationId, CancellationToken ct = default)
    {
        var token = await GetInstallationTokenAsync(installationId, ct);

        return new GitHubClient(new ProductHeaderValue("AdrRegistry"))
        {
            Credentials = new Credentials(token)
        };
    }
}
