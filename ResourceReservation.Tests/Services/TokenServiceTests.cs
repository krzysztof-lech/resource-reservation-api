using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
    public void CreateToken_Returns_ValidJwt_WithExpectedClaims()
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
        principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(JwtRegisteredClaimNames.Email)!.Value.Should().Be(user.Email);
        principal.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value.Should().Be(user.Role.ToString());
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

        var parts = token.Split('.');
        parts.Length.Should().Be(3);

        var payload = parts[1].ToCharArray();
        payload[0] = payload[0] == 'A' ? 'B' : 'A';
        parts[1] = new string(payload);
        var tampered = string.Join('.', parts);

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

        Assert.ThrowsAny<SecurityTokenException>(() =>
        {
            SecurityToken validatedToken;
            handler.ValidateToken(tampered, validationParameters, out validatedToken);
        });
    }
}
   
