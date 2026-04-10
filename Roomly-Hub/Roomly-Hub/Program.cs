using Application.Common.Mappers;
using Application.DTOs;
using Application.DTOs.Payment;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Services;
using Application.Services.Bookings;
using Application.Services.RoomCRUD;
using Application.Validators;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Payment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Roomly_Hub.Middleware;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddHttpClient();

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
    var secretKey = builder.Configuration["JwtSettings:SecretKey"];
    if (string.IsNullOrWhiteSpace(secretKey))
    {
        throw new InvalidOperationException("JWT SecretKey is missing in configuration.");
    }

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection(GoogleSettings.SectionName));
builder.Services.Configure<FawaterakOptions>(builder.Configuration.GetSection("Fawaterak"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHasher, BCryptHasher>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IKycSubmissionRepository, KycSubmissionRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPaymentService, FawaterakPaymentService>();

builder.Services.AddScoped<IRoomMapper, RoomMapper>();
builder.Services.AddScoped<IBookingMapper, BookingMapper>();

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
builder.Services.AddScoped<IBookingServices, BookingServices>();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the text box below. Example: 12345abcdef"
    });
});
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
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

/*/
 "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkNzRhNmI0Ni1lZGE3LTRiMTUtYjAwYi1jOThkNDM5YjQwMjEiLCJlbWFpbCI6Im9zb3NtbzI0MkBnbWFpbC5jb20iLCJuYW1lIjoib3NhbWEiLCJqdGkiOiI3ZjBiMDI4Ni05ZGIwLTRkMGYtOTY0MC03ODc0NTUwOGVlMWQiLCJyb2xlIjoiR3Vlc3QiLCJuYmYiOjE3NzU4NDg1MzksImV4cCI6MTc3NTg1MjEzOSwiaWF0IjoxNzc1ODQ4NTM5LCJpc3MiOiJSb29tbHktSHViIiwiYXVkIjoiUm9vbWx5LUh1Yi1Vc2VycyJ9.G95bFT0Z2ky_te-tmi7QrFZGBhC5VIOrdAC1sTYV4Jg",
  "refreshToken": "N+UL3krx7kMZ7OVVWQfxiARbL3xK1rY4LIxu0y/kZJ0Nf9oq64viyZRdHAl5+BMkLNXfk25zTn6EIHAiB/6g7w==",

 */