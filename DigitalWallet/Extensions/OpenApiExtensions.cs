using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace DigitalWallet.API.Extensions
{
    public static class OpenApiExtensions
    {
        public static IServiceCollection AddCustomOpenApi(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                    // إعدادات JWT Security Scheme
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter 'Bearer' [space] and then your valid token."
                    };

                    // ربط الـ Security بكل الـ Endpoints تلقائياً
                    document.Security ??= new List<OpenApiSecurityRequirement>();
                    document.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                    });

                    return Task.CompletedTask;
                });
            });

            return services;
        }

        public static WebApplication UseCustomScalar(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi(); // لتوليد ملف الـ openapi.json

                app.MapScalarApiReference(options =>
                {
                    options
                        .WithTitle("Digital Wallet API")
                        .WithTheme(ScalarTheme.Mars); // يمكنك اختيار الثيم المفضل لديك
                });
            }

            return app;
        }
    }
}