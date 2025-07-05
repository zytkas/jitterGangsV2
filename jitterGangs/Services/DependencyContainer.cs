using JitterGang.Services;
using JitterGang.Services.Input;
using JitterGang.ViewModels;
using jitterGangs.Admin;
using jitterGangs.Services;
using jitterGangs.Services.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace jitterGangs
{
    public static class DependencyContainer
    {
        private static ServiceProvider _serviceProvider;

        public static void Initialize()
        {
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IJitterService, JitterService>();
            services.AddSingleton<IFirebaseService, FirebaseService>();
            services.AddSingleton<IDialogService, DialogService>();
            // Register ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<LicenseManagerViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }

        public static T Resolve<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}