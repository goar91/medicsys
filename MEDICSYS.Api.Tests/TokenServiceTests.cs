using Microsoft.Extensions.Configuration;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Tests;

public class TokenServiceTests
{
    [Fact]
    public void CreateToken_ReturnsTokenWithFutureExpiration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "MEDICSYS",
            ["Jwt:Audience"] = "MEDICSYS",
            ["Jwt:Key"] = "THIS_IS_A_SUPER_SECURE_TEST_KEY_1234567890",
            ["Jwt:ExpiresMinutes"] = "5"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var service = new TokenService(configuration);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@medicsys.local",
            UserName = "test@medicsys.local"
        };

        var (token, expiresAt) = service.CreateToken(user, new List<string> { "Odontologo" });

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow.AddMinutes(4));
    }

    [Fact]
    public void CreateToken_ThrowsWhenKeyMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new TokenService(configuration);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@medicsys.local",
            UserName = "test@medicsys.local"
        };

        Assert.Throws<InvalidOperationException>(() => service.CreateToken(user, new List<string>()));
    }
}
