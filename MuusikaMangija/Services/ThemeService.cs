namespace MuusikaMangija.Services;

public class ThemeService
{
	private const string ThemeKey = "SelectedTheme";

	public static void Initialize()
	{
		string savedTheme = Preferences.Default.Get(ThemeKey, "Light");
		ApplyTheme(savedTheme);
	}

	public static void ApplyTheme(string theme)
	{
		Preferences.Default.Set(ThemeKey, theme);

		if (Application.Current != null)
		{
			Application.Current.UserAppTheme = theme switch
			{
				"Dark" => AppTheme.Dark,
				_ => AppTheme.Light
			};
		}
	}

	public static string GetCurrentTheme() => Preferences.Default.Get(ThemeKey, "Light");
}
