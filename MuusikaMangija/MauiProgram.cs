using Microsoft.Extensions.Logging;
using MuusikaMangija.Services;
using Plugin.Maui.Audio;
using MuusikaMangija.ViewModels;
using MuusikaMangija.Views;

namespace MuusikaMangija
{
	public static partial class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			// Register services
			builder.Services.AddSingleton<DatabaseService>();
			builder.Services.AddSingleton<IAudioScanner, DefaultAudioScanner>();
		// Register audio services
		builder.Services.AddSingleton(AudioManager.Current);
		builder.Services.AddSingleton<AudioService>();

			// Register pages and viewmodels
			builder.Services.AddTransient<AudioPlayerViewModel>();
			builder.Services.AddTransient<AudioPlayerPage>();
         builder.Services.AddTransient<HiddenSongsViewModel>();
			builder.Services.AddTransient<HiddenSongsPage>();
			builder.Services.AddTransient<SettingsViewModel>();
			builder.Services.AddTransient<SettingsPage>();

			// Allow platform-specific registrations
			RegisterPlatformServices(builder);

			var mauiApp = builder.Build();

			// Save service provider for simple DI resolution in code-behind
			try
			{
				App.Services = mauiApp.Services;
			}
			catch { }

			return mauiApp;
		}

		static partial void RegisterPlatformServices(MauiAppBuilder builder);
	}
}
