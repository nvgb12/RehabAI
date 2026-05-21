using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RehabAI.Api.Authorization;
using RehabAI.Api.Swagger;
using RehabAI.Application.Chatbot;
using RehabAI.Infrastructure;
using RehabAI.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);
const string LocalFrontendCorsPolicy = "LocalFrontendCorsPolicy";
var localFrontendOrigins = new[]
{
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://localhost:5173",
    "https://127.0.0.1:5173"
};

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalFrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(localFrontendOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<UpdateOrderStatusRequestSchemaFilter>();
    options.OperationFilter<AuthorizeOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT access token from POST /api/Auth/login."
    });
});
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RehabAI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RehabAI.Client";
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ??
    "development-only-rehabai-local-signing-key-change-before-production";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "roles"
        };
    });
builder.Services.AddScoped<IAuthorizationHandler, ActiveRoleAuthorizationHandler>();
builder.Services.AddScoped<IEndpointAccessService, EndpointAccessService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AccessPolicies.ActivePatient,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveRoleRequirement(AccessPolicies.PatientRole)));

    options.AddPolicy(
        AccessPolicies.ActiveAdmin,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveRoleRequirement(AccessPolicies.AdminRole)));

    options.AddPolicy(
        AccessPolicies.ActiveDoctorStaffOrAdmin,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveRoleRequirement(
                AccessPolicies.DoctorRole,
                AccessPolicies.AdminRole,
                AccessPolicies.AuthorizedInternalStaffRole,
                AccessPolicies.SupportStaffRole,
                AccessPolicies.VerificationAdminRole)));
});
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.SeedDatabaseAsync(app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(LocalFrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
