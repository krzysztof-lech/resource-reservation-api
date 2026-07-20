using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Security;
using ResourceReservation.Api.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Xunit;

namespace ResourceReservation.Tests.Services;
public class TokenServiceTests
{
    [Fact]
    public void CreateToken_ValidatesSignatureAndClaims()
    {
        var settings = new JwtSettings
        {
            Key = "supersecretkey_supersecretkey_123456",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = 60
        };

        var svc = new TokenService(Options.Create(settings));
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Admin",
            Email = "alice@example.com",
            PasswordHash = PasswordHasher.HashPassword("irrelevant"),
            Role = UserRole.Admin
        };

        var token = svc.CreateToken(user);
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            ClockSkew = TimeSpan.Zero
        };

        SecurityToken validatedToken;
        var principal = handler.ValidateToken(token, validationParameters, out validatedToken);

        principal.Should().NotBeNull();

        var subClaim = principal.Claims.FirstOrDefault(c =>
           c.Type == JwtRegisteredClaimNames.Sub ||
           c.Type == "sub" ||
           c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

        var emailClaim = principal.Claims.FirstOrDefault(c =>
            c.Type == JwtRegisteredClaimNames.Email ||
            c.Type == "email" ||
            c.Type == System.Security.Claims.ClaimTypes.Email);

        var roleClaim = principal.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.Role ||
            c.Type == "role" ||
            c.Type == "roles");

        subClaim.Should().NotBeNull("token must contain subject (sub / name identifier)");
        emailClaim.Should().NotBeNull("token must contain email");
        roleClaim.Should().NotBeNull("token must contain role");

        subClaim!.Value.Should().Be(user.Id.ToString());
        emailClaim!.Value.Should().Be(user.Email);
        roleClaim!.Value.Should().Be(user.Role.ToString());
    }

    [Fact]
    public void CreateToken_SetsExpiration_AccordingToSettings()
    {
        var settings = new JwtSettings
        {
            Key = "another_super_secret_key_for_tests_0123",
            Issuer = "iss",
            Audience = "aud",
            ExpiryMinutes = 30
        };

        var svc = new TokenService(Options.Create(settings));
        var user = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "User",
            Email = "bob@example.com",
            PasswordHash = PasswordHasher.HashPassword("irrelevant"),
            Role = UserRole.User 
        };

        var token = svc.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
    }
}
   
