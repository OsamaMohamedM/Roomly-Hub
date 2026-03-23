using Application.Common.Mappers;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Services;
using Application.Services.RoomCRUD;
using Application.Validators;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Roomly_Hub.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Infrastructure")
    ));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
    };
});
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection(GoogleSettings.SectionName));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHasher, BCryptHasher>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IKycSubmissionRepository, KycSubmissionRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


builder.Services.AddScoped<IRoomMapper, RoomMapper>();

builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IGoogleLoginService, GoogleLoginService>();
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IRoomModerationService, RoomModerationService>();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();