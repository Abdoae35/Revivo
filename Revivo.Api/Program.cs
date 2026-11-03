using Revivo.Infrastructure;
using Revivo.Api.Authentication;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
    string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32 ||
    !int.TryParse(builder.Configuration["Jwt:ExpirationMinutes"], out var expirationMinutes) || expirationMinutes <= 0)
    throw new InvalidOperationException("JWT issuer, audience, positive expiration, and a signing key of at least 32 UTF-8 bytes are required.");

var tokenService = new JwtTokenService(issuer, audience, signingKey, expirationMinutes);
builder.Services.AddSingleton(tokenService);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = tokenService.CreateValidationParameters();
});
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapRegistrationEndpoints();
app.MapLoginEndpoints();
app.MapCurrentUserEndpoints();

app.Run();
