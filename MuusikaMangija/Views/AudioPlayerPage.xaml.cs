using System;
using Microsoft.Maui.Controls;
using MuusikaMangija.ViewModels;
using MuusikaMangija.Models;
using MuusikaMangija.Services;

namespace MuusikaMangija.Views;

public partial class AudioPlayerPage : ContentPage
{
	public AudioPlayerPage(AudioPlayerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	// Handle song tap: animate and play
	private async void OnSongTapped(object sender, EventArgs e)
	{
		try
		{
			if (sender is Border border && border.BindingContext is Song song && BindingContext is AudioPlayerViewModel vm)
			{
				// simple scale animation for feedback
				await border.ScaleTo(0.97, 60);
				await border.ScaleTo(1.0, 120, Easing.SpringOut);

				// play the song (PlaySongCommand)
				if (vm.PlaySongCommand.CanExecute(song))
					vm.PlaySongCommand.Execute(song);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnSongTapped error: {ex}");
		}
	}

	// Menu button tapped on an item — show ActionSheet with options
	private async void OnMenuClicked(object sender, EventArgs e)
	{
		try
		{
			if (sender is Button btn && btn.CommandParameter is Song song && BindingContext is AudioPlayerViewModel vm)
			{
                var add = LocalizationManager.Instance["Menu_Add"];
				var addPlay = LocalizationManager.Instance["Menu_AddPlay"];
				var hide = LocalizationManager.Instance["Menu_Hide"];
				var cancel = LocalizationManager.Instance["Action_Cancel"];

				var action = await DisplayActionSheet(song.Title, cancel, null, add, addPlay, hide);
				if (action == add)
				{
					if (vm.AddToQueueCommand.CanExecute(song)) vm.AddToQueueCommand.Execute(song);
				}
				else if (action == addPlay)
				{
					if (vm.AddToQueueCommand.CanExecute(song)) vm.AddToQueueCommand.Execute(song);
					if (vm.PlaySongCommand.CanExecute(song)) vm.PlaySongCommand.Execute(song);
				}
				else if (action == hide)
				{
					if (vm.HideSongCommand.CanExecute(song)) vm.HideSongCommand.Execute(song);
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OnMenuClicked error: {ex}");
		}
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
		if (sender is BindableObject bo && bo.BindingContext is Song song)
		{
			System.Diagnostics.Debug.WriteLine($"Dragging: {song.Title}");

			if (BindingContext is AudioPlayerViewModel vm)
				vm.DragStartingCommand.Execute(song);
		}
	}
	// Drop handler for the drop target Border
	void OnDrop(object sender, DropEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("DROP DETECTED");

		if (BindingContext is AudioPlayerViewModel vm)
		{
			vm.DropCommand.Execute(null);
		}
	}

	private Song? _queueDraggedSong;

	void OnQueueDragStarting(object sender, DragStartingEventArgs e)
	{
		if (sender is BindableObject bo &&
			bo.BindingContext is Song song)
		{
			_queueDraggedSong = song;
		}
	}

	void OnQueueDrop(object sender, DropEventArgs e)
	{
		if (sender is BindableObject bo &&
			bo.BindingContext is Song targetSong &&
			BindingContext is AudioPlayerViewModel vm &&
			_queueDraggedSong != null)
		{
			var oldIndex = vm.PlaybackQueue.IndexOf(_queueDraggedSong);
			var newIndex = vm.PlaybackQueue.IndexOf(targetSong);

			if (oldIndex >= 0 && newIndex >= 0)
			{
				vm.PlaybackQueue.Move(oldIndex, newIndex);
			}
		}
	}

	private async void OnSettingsClicked(object sender, EventArgs e)
	{
		// Make sure your application uses a NavigationPage structure to push pages
		await Navigation.PushAsync(new SettingsPage());
	}
}
