using System;
using Microsoft.Maui.Controls;
using MuusikaMangija.ViewModels;
using MuusikaMangija.Models;

namespace MuusikaMangija.Views;

public partial class AudioPlayerPage : ContentPage
{
	public AudioPlayerPage(AudioPlayerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnHiddenClicked(object sender, EventArgs e)
	{
		try
		{
			var page = App.Services?.GetService(typeof(HiddenSongsPage)) as Page;
			if (page != null)
			{
				await Shell.Current.Navigation.PushAsync(page);
			}
			else
			{
				await DisplayAlert("Error", "Hidden songs page not available.", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnHiddenClicked error: {ex}");
			await DisplayAlert("Error", "Could not open hidden songs.", "OK");
		}
	}

	// Define this custom permission class at the bottom of your file or in your Services folder
	public class ReadAudioPermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
	{
#if ANDROID
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
			new[] { (Android.Manifest.Permission.ReadMediaAudio, true) };
#endif
	}

	// Update your button click method:
	private async void OnScanClicked(object sender, EventArgs e)
	{
		PermissionStatus status;

		// Check for Android 13+ specific permission
		if (DeviceInfo.Version.Major >= 13)
		{
			status = await Permissions.CheckStatusAsync<ReadAudioPermission>();
			if (status != PermissionStatus.Granted)
			{
				status = await Permissions.RequestAsync<ReadAudioPermission>();
			}
		}
		else
		{
			status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
			if (status != PermissionStatus.Granted)
			{
				status = await Permissions.RequestAsync<Permissions.StorageRead>();
			}
		}

		if (status == PermissionStatus.Granted)
		{
			if (BindingContext is AudioPlayerViewModel vm)
			{
				await vm.ScanDeviceAsyncPublic();
			}
		}
		else
		{
			await DisplayAlert("Luba puudub", "Rakendus vajab seadme muusikafailide leidmiseks luba.", "OK");
		}
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
		}
	}

	// Drag starting from each item Border; sender is the Border in the DataTemplate
	void OnDragStarting(object sender, DragStartingEventArgs e)
	{
		try
		{
			if (sender is BindableObject bo && bo.BindingContext is Song song && BindingContext is AudioPlayerViewModel vm)
			{
				// store drag in VM (fallback for platforms that don't propagate DataPackage)
				if (vm.DragStartingCommand.CanExecute(song))
					vm.DragStartingCommand.Execute(song);

                // also set payload so DropEventArgs can read it
				try
				{
					if (e.Data?.Properties != null)
						e.Data.Properties["SongId"] = song.Id;
				}
				catch { }
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnDragStarting error: {ex}");
		}
	}

	// Drop handler for the drop target Border
	void OnDrop(object sender, DropEventArgs e)
	{
		try
		{
			if (BindingContext is AudioPlayerViewModel vm)
			{
				// Try read text payload (song id)
                try
				{
					if (e.Data?.Properties != null && e.Data.Properties.TryGetValue("SongId", out var val))
					{
						if (val != null && int.TryParse(val.ToString(), out var id))
						{
							var song = vm.AllSongs.FirstOrDefault(s => s.Id == id);
							if (song != null)
							{
								vm.PlaybackQueue.Add(song);
								return;
							}
						}
					}
				}
				catch { }

				// fallback: call VM.DropCommand which uses the stored _draggedSong
				if (vm.DropCommand.CanExecute(null))
					vm.DropCommand.Execute(null);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnDrop error: {ex}");
		}
	}
}
