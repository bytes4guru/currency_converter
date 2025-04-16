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
using CurrencyConverter.Porviders;
using CurrencyConverter.Configurations;

public partial class Program
{
    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var env = builder.Environment;

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Logging
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
        });

        // Services
        

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

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtIssuerOptions:SecretKey"]!));

        // Register the hashed key for reuse
       


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
       
        builder.Services.AddMemoryCache();

        builder.Services.Configure<ExchangeRateApiSettings>(builder.Configuration.GetSection("ExchangeRateApi"));
        builder.Services.Configure<ExcludedCurrenciesSettings>(builder.Configuration.GetSection("ExcludedCurrencies"));
        // Bind & validate the settings
        var exchangeRateApiSettings = builder.Configuration
            .GetSection("ExchangeRateApi")
            .Get<ExchangeRateApiSettings>();

        if (exchangeRateApiSettings == null || !exchangeRateApiSettings.Providers.Any())
        {
            throw new InvalidOperationException("ExchangeRateApi settings or providers are missing or empty. Please check your configuration.");
        }

       
        foreach (var provider in exchangeRateApiSettings.Providers)
        {
            builder.Services.AddHttpClient(provider.Name, client =>
            {
                client.BaseAddress = new Uri(provider.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(
                    provider.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(provider.RetryBackoffSeconds, retryAttempt))))
            .AddTransientHttpErrorPolicy(policy =>
                policy.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: provider.CircuitBreakerFailureCount,
                    durationOfBreak: TimeSpan.FromSeconds(provider.CircuitBreakerDurationSeconds)));
        }



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

        builder.Services.AddSingleton<SecurityKey>(signingKey);
        builder.Services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
        builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddTransient<IAuthService, SimpleAuthService>();
        builder.Services.AddTransient<FrankfurterProvider>();
        
        builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new GlobalRoutePrefixConvention("api"));
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

