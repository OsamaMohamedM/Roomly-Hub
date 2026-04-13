using Application.Common.Mappers;
using Application.DTOs;
using Application.DTOs.Payment;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Bookings;
using Application.Services;
using Application.Services.Bookings;
using Application.Services.Payments;
using Application.Services.RoomCRUD;
using Application.Validators;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Payment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Roomly_Hub.Common;
using Roomly_Hub.Middleware;
using Serilog;
using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var allErrors = context.ModelState
            .Where(x => x.Value is { Errors.Count: > 0 })
            .SelectMany(x => x.Value!.Errors)
            .ToList();

        var hasDataTypeError = allErrors.Any(e =>
            e.Exception is JsonException or FormatException ||
            (!string.IsNullOrWhiteSpace(e.ErrorMessage) &&
             e.ErrorMessage.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)));

        var hasMissingAttributesError = allErrors.Any(e =>
            !string.IsNullOrWhiteSpace(e.ErrorMessage) &&
            (e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase) ||
             e.ErrorMessage.Contains("was not provided", StringComparison.OrdinalIgnoreCase) ||
             e.ErrorMessage.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase)));

        var detail = hasDataTypeError
            ? "Invalid data type in request payload."
            : hasMissingAttributesError
                ? "There are missing attributes in the request payload."
                : "Request validation failed.";

        var errorType = hasDataTypeError
            ? "DATA_TYPE_MISMATCH"
            : hasMissingAttributesError
                ? "MISSING_ATTRIBUTES"
                : "VALIDATION_ERROR";

        var problem = new ApiProblemDetails
        {
            Code = "VALIDATION_ERROR",
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = detail,
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["errorType"] = errorType;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
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
builder.Services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();

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
builder.Services.AddScoped<IBookingPaymentRequestFactory, BookingPaymentRequestFactory>();
builder.Services.AddScoped<IBookingCommandService, BookingCommandService>();
builder.Services.AddScoped<IBookingQueryService, BookingQueryService>();
builder.Services.AddScoped<IBookingPaymentFlowService, BookingPaymentFlowService>();
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
