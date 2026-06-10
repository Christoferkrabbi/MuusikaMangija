using MuusikaMangija.ViewModels;
// removed media playback code that referenced MediaElement

namespace MuusikaMangija.Views;

public partial class AudioPlayerPage : ContentPage
{
	public AudioPlayerPage(AudioPlayerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (BindingContext is AudioPlayerViewModel vm)
		{
			try
			{
				await vm.InitializeAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"AudioPlayerPage initialize error: {ex}");
			}

                // subscribe to CurrentSong changes if needed (no in-app player wired)
				// vm.PropertyChanged += Vm_PropertyChanged;
		}
	}
	// private void Vm_PropertyChanged(...) removed
}