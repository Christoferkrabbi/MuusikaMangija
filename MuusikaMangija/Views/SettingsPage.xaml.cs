using MuusikaMangija.Services;

namespace MuusikaMangija.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
	}

	private async void OnToolbarMenuClicked(object sender, EventArgs e)
	{
		// Simple action sheet with a placeholder action. Localize if needed.
		var edit = LocalizationManager.Instance["Edit"] ?? "Edit";
		var reset = LocalizationManager.Instance["ResetSettings"] ?? "Reset";
		var cancel = LocalizationManager.Instance["Action_Cancel"] ?? "Cancel";

		var action = await DisplayActionSheet(LocalizationManager.Instance["AppInfo"] ?? "Settings", cancel, null, edit, reset);
		if (action == edit)
		{
			await DisplayAlert(edit, "No edit action implemented.", "OK");
		}
		else if (action == reset)
		{
			var ok = await DisplayAlert(reset, LocalizationManager.Instance["ConfirmReset"] ?? "Reset all settings?", LocalizationManager.Instance["Yes"] ?? "Yes", LocalizationManager.Instance["No"] ?? "No");
			if (ok)
			{
				// Reset sample: clear saved language and theme preferences
				Preferences.Default.Remove("SavedLanguage");
				Preferences.Default.Remove("SavedTheme");
				ThemeService.ApplyTheme("Light");
				await DisplayAlert(LocalizationManager.Instance["AppInfo"] ?? "Info", LocalizationManager.Instance["SettingsResetDone"] ?? "Settings reset.", "OK");
			}
		}
	}
}
