using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Revivo.Api.Authentication;
using Revivo.Application.Authentication;

namespace Revivo.Api.Tests;

public class AuthenticationTests
{
    // Synthetic key used only to test token signing; it is not a deployed secret.
    private const string TestKey = "test-only-signing-key-with-32-bytes-minimum";
    private readonly JwtTokenService _tokens = new("test-issuer", "test-audience", TestKey, 5);

    [Fact]
    public void TokenContainsUserClaimsAndPassesValidation()
    {
        var user = new AuthenticatedUser(Guid.NewGuid(), "user@example.invalid");
        var token = _tokens.CreateToken(user);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, _tokens.CreateValidationParameters(), out _);
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(user.Email, principal.FindFirstValue(JwtRegisteredClaimNames.Email));
    }

    [Fact]
    public void TokenRejectsWrongIssuerAndSigningKey()
    {
        var token = _tokens.CreateToken(new(Guid.NewGuid(), "user@example.invalid"));
        var handler = new JwtSecurityTokenHandler();
        var wrongIssuer = _tokens.CreateValidationParameters();
        wrongIssuer.ValidIssuer = "wrong";
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => handler.ValidateToken(token, wrongIssuer, out _));
        var wrongKey = _tokens.CreateValidationParameters();
        wrongKey.IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("another-test-only-signing-key-with-32-bytes"));
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, wrongKey, out _));
    }

    [Fact]
    public void TokenRejectsExpiredLifetimeAndWrongAudience()
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = _tokens.CreateValidationParameters();
        var token = _tokens.CreateToken(new(Guid.NewGuid(), "user@example.invalid"));
        parameters.ValidAudience = "wrong";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(token, parameters, out _));
        parameters = _tokens.CreateValidationParameters();
        var expired = new JwtSecurityToken("test-issuer", "test-audience", expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: new SigningCredentials(parameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256));
        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(handler.WriteToken(expired), parameters, out _));
    }

    [Theory]
    [InlineData(false, 401)]
    [InlineData(true, 200)]
    public async Task LoginReturnsExpectedStatus(bool valid, int status)
    {
        var method = typeof(LoginEndpoints).GetMethod("LoginAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var task = (Task<IResult>)method.Invoke(null, [new LoginRequest("user@example.invalid", "not-a-real-password"), new FakeLogin(valid), _tokens])!;
        Assert.Equal(status, ((IStatusCodeHttpResult)await task).StatusCode);
    }

    [Fact]
    public async Task CurrentUserUsesTokenSubjectAndReturnsOnlyProfile()
    {
        var profile = new UserProfile(Guid.NewGuid(), "Test User", "user@example.invalid");
        var token = _tokens.CreateToken(new AuthenticatedUser(profile.Id, profile.Email!));
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
            .ValidateToken(token, _tokens.CreateValidationParameters(), out _);
        var result = await InvokeCurrentUser(principal, new ProfileLogin(profile));
        Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);
        var value = ((IValueHttpResult)result).Value!;
        Assert.Equal(profile.Id, value.GetType().GetProperty("Id")!.GetValue(value));
        Assert.Equal(profile.Name, value.GetType().GetProperty("Name")!.GetValue(value));
        Assert.Equal(profile.Email, value.GetType().GetProperty("Email")!.GetValue(value));
        Assert.Null(value.GetType().GetProperty("PasswordHash"));
    }

    [Fact]
    public async Task CurrentUserRejectsMissingSubjectOrMissingUser()
    {
        var noSubject = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Equal(401, ((IStatusCodeHttpResult)await InvokeCurrentUser(noSubject, new ProfileLogin(null))).StatusCode);
        var subject = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())]));
        Assert.Equal(401, ((IStatusCodeHttpResult)await InvokeCurrentUser(subject, new ProfileLogin(null))).StatusCode);
    }

    private static Task<IResult> InvokeCurrentUser(ClaimsPrincipal principal, ILoginService service)
    {
        var method = typeof(CurrentUserEndpoints).GetMethod("GetCurrentUserAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (Task<IResult>)method.Invoke(null, [principal, service])!;
    }

    private sealed class ProfileLogin(UserProfile? profile) : ILoginService
    {
        public Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password) => Task.FromResult<AuthenticatedUser?>(null);
        public Task<UserProfile?> GetUserAsync(Guid userId) => Task.FromResult(profile?.Id == userId ? profile : null);
    }

    private sealed class FakeLogin(bool valid) : ILoginService
    {
        public Task<UserProfile?> GetUserAsync(Guid userId) => Task.FromResult<UserProfile?>(null);
        public Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password) =>
            Task.FromResult(valid ? new AuthenticatedUser(Guid.NewGuid(), email) : null);
    }
}
