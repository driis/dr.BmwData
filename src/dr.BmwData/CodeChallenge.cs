using System.Security.Cryptography;
using System.Text;

namespace dr.BmwData;

public sealed class CodeChallenge
{
    public string Challenge { get; private set; } = null!;
    public string Verification { get; private set; } = null!;
    
    public CodeChallenge()
    {
        Verification = GenerateCodeVerifier();
        Challenge = GenerateCodeChallenge(Verification);
    }

    private string GenerateCodeVerifier()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {      
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(bytes);
        return Base64UrlEncode(challengeBytes);
    }

    private string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}