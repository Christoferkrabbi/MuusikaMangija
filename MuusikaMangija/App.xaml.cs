using System.Globalization;
using MuusikaMangija.Services;

namespace MuusikaMangija;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// 1. Initialize configurations
		ThemeService.Initialize();
		string savedLang = Preferences.Default.Get("SavedLanguage", "et");
		LocalizationManager.Instance.CurrentCulture = new CultureInfo(savedLang);

        // 2. Set AppShell as the main gateway
		MainPage = new AppShell();
	}

	// Expose the application service provider for simple DI resolution from XAML code-behind
	public static IServiceProvider? Services { get; internal set; }
}
