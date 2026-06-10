using Microsoft.Extensions.DependencyInjection;
using MuusikaMangija.Services;
using MuusikaMangija.Views;

namespace MuusikaMangija;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		ThemeService.Initialize();

		// Replace Shell items with AudioPlayerPage resolved from DI so the audio player is the first page
        // Defer page creation to the main thread to avoid platform initialization timing issues.
		try
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					// Use a DataTemplate factory that resolves the page from the DI container when needed.
					var template = new DataTemplate(() => App.Services?.GetService<AudioPlayerPage>() as Page);

					Items.Clear();
					Items.Add(new ShellContent
					{
						ContentTemplate = template,
						Title = "MuusikaMangija",
						Route = "audioplayer"
					});
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"AppShell page resolution error: {ex}");
				}
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"AppShell scheduling error: {ex}");
		}
	}
}
