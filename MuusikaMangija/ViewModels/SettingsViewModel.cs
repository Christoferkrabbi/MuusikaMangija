using System.Globalization;
using System.Windows.Input;
using MuusikaMangija.Services;

namespace MuusikaMangija.ViewModels;

public class SettingsViewModel
{
	public ICommand ChangeThemeCommand { get; }
	public ICommand ChangeLanguageCommand { get; }

	public SettingsViewModel()
	{
		// Change Theme and instantly save via Preferences
		ChangeThemeCommand = new Command<string>((theme) =>
		{
			if (!string.IsNullOrEmpty(theme))
			{
				ThemeService.ApplyTheme(theme);
			}
		});

		// Change Language instantly without restart
		ChangeLanguageCommand = new Command<string>((langCode) =>
		{
			if (!string.IsNullOrEmpty(langCode))
			{
				// Save preference for next startup
				Preferences.Default.Set("SavedLanguage", langCode);

				// Trigger live UI update
				LocalizationManager.Instance.CurrentCulture = new CultureInfo(langCode);
			}
		});
	}
}
