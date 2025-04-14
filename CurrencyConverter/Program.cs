using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using CurrencyConverter.Services;
using CurrencyConverter.Middlewares;
using CurrencyConverter.Conventions;
using Polly;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text.Json;
using OpenTelemetry.Resources;

public partial class Program
{
    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        // Logging
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
        });

        // Services
        builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new GlobalRoutePrefixConvention("api"));
        });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Currency Converter API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    new string[] { }
                }
            });
        });

        // JWT Auth

        var jwtKey = Encoding.UTF8.GetBytes(builder.Configuration["JwtIssuerOptions:SecretKey"]!);
        var signingKey = new SymmetricSecurityKey(jwtKey);

        // Register the hashed key for reuse
        builder.Services.AddSingleton<SecurityKey>(signingKey);


        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =signingKey
                };
            });


        builder.Services.AddAuthorization(options => {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
        });


        // Custom Services
        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IExchangeRateService, FrankfurterExchangeRateService>();
        builder.Services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddTransient<IAuthService, SimpleAuthService>();


        builder.Services.AddHttpClient("Frankfurter", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ExchangeRateApi:BaseUrl"]!);
        }).AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(int.Parse(builder.Configuration["ExchangeRateApi:RetryCount"]!),
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(int.Parse(builder.Configuration["ExchangeRateApi:RetryBackoffSeconds"]!), retryAttempt))))
        .AddTransientHttpErrorPolicy(policy =>
            policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));


        builder.Services.AddOpenTelemetry()
     .WithTracing(tracerProviderBuilder =>
     {
         tracerProviderBuilder
             .SetResourceBuilder(
                 ResourceBuilder.CreateDefault()
                     .AddService("CurrencyConverterAPI")) // Logical service name for traces
             .AddAspNetCoreInstrumentation()
             .AddHttpClientInstrumentation()
             .AddConsoleExporter();
     });

        builder.Services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var error = new
                {
                    status = 429,
                    message = "Rate limit exceeded. Please wait before making more requests.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                        ? $"{retry.TotalSeconds}s"
                        : null
                };

                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(error), token);
            };
            options.AddPolicy("PerUserPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = int.Parse(builder.Configuration["RateLimiting:PermitLimit"]!),
                        Window = TimeSpan.FromSeconds(int.Parse(builder.Configuration["RateLimiting:WindowSeconds"]!)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = int.Parse(builder.Configuration["RateLimiting:QueueLimit"]!)
                    }));
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.UseMiddleware<RequestLoggingMiddleware>();

        app.MapControllers().RequireRateLimiting("PerUserPolicy");
        return app;

        
    }

    public static void Main(string[] args)
    {
        var app = CreateApp(args);
        app.Run();
    }
}

