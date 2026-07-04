using Microsoft.AspNetCore.Localization;
using System.Globalization;
namespace API.Extentions
{
    public static class LocalizationExtentions
    {
        private const string DefaultCulture = "en-US";

        private static readonly string[] SupportedCultures = { "en-US", "ar-EG" };

        // ── AddLocalizationServices ───────────────────────────────────────────────
        // Call this in Program.cs: builder.Services.AddLocalizationServices();

        public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
        {
            // Tells ASP.NET Core where to look for .resx files.
            // "Resources" is the folder path relative to the Localization project.
            services.AddLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = SupportedCultures
                    .Select(c => new CultureInfo(c))
                    .ToList();

                options.DefaultRequestCulture = new RequestCulture(DefaultCulture);
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;

                // Order matters — first provider that returns a culture wins.
                options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                // 1. Query string:  GET /api/profile?culture=ar-EG
                new QueryStringRequestCultureProvider(),

                // 2. Accept-Language header (standard browser/mobile header)
                new AcceptLanguageHeaderRequestCultureProvider(),

                // 3. Cookie (set by the frontend after the user picks a language)
                new CookieRequestCultureProvider()
            };
            });

            return services;
        }

        // ── UseLocalizationServices ───────────────────────────────────────────────
        // Call this in Program.cs after app.UseRouting(): app.UseLocalizationServices();

        public static IApplicationBuilder UseLocalizationServices(this IApplicationBuilder app)
        {
            app.UseRequestLocalization();
            return app;
        }
    }
}