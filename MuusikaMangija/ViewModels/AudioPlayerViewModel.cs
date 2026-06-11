using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MuusikaMangija.Models;
using MuusikaMangija.Services;

namespace MuusikaMangija.ViewModels;

public class AudioPlayerViewModel : INotifyPropertyChanged
{
	private readonly DatabaseService _databaseService;
	private readonly IAudioScanner _audioScanner;
	private readonly AudioService _audioService;

	private Song? _currentSong;
	private bool _isPlaying;
	private Song? _draggedSong;
	private bool _isBusy;
	private string _statusMessage = string.Empty;

	public ObservableCollection<Song> AllSongs { get; } = new();
	public ObservableCollection<Song> PlaybackQueue { get; } = new();

	public Song? CurrentSong
	{
		get => _currentSong;
		set
		{
			if (_currentSong == value) return;
			_currentSong = value;
			OnPropertyChanged(nameof(CurrentSong));
		}
	}

	public bool IsPlaying
	{
		get => _isPlaying;
		set
		{
			if (_isPlaying == value) return;
			_isPlaying = value;
			OnPropertyChanged(nameof(IsPlaying));
		}
	}

	// Visual State Properties
	public bool IsBusy
	{
		get => _isBusy;
		set
		{
			if (_isBusy == value) return;
			_isBusy = value;
			OnPropertyChanged(nameof(IsBusy));
		}
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set
		{
			if (_statusMessage == value) return;
			_statusMessage = value;
			OnPropertyChanged(nameof(StatusMessage));
		}
	}

	public ICommand PlayPauseCommand { get; }
	public ICommand PlaySongCommand { get; }
	public ICommand ToggleFavoriteCommand { get; }
	public ICommand DragStartingCommand { get; }
	public ICommand DropCommand { get; }
	public ICommand ScanDeviceCommand { get; }
	public ICommand HideSongCommand { get; }
	public ICommand PickFileCommand { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public AudioPlayerViewModel(DatabaseService databaseService, IAudioScanner audioScanner, AudioService audioService)
	{
		_databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
		_audioScanner = audioScanner ?? throw new ArgumentNullException(nameof(audioScanner));
		_audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

		PlayPauseCommand = new Command(async () => await PlayPauseAsync());
		PlaySongCommand = new Command<Song>(async (s) => await PlaySongAsync(s));
		ToggleFavoriteCommand = new Command<Song>(async (s) => await ToggleFavoriteAsync(s));
		DragStartingCommand = new Command<Song>(OnDragStarting);
		DropCommand = new Command(async () => await OnDropAsync());
		ScanDeviceCommand = new Command(async () => await ScanDeviceAsync());
		HideSongCommand = new Command<Song>(async (s) => await HideSongAsync(s));
		PickFileCommand = new Command(async () => await PickFileAsync());
	}

	public Task InitializeAsync()
	{
		return InitializeInternalAsync();
	}

	private async Task InitializeInternalAsync()
	{
		try
		{
			await _databaseService.ImportUserSongsAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ImportUserSongsAsync failed: {ex}");
		}

		await LoadMusicLibraryAsync();
	}

	private void OnDragStarting(Song song)
	{
		_draggedSong = song;
	}

	private async Task OnDropAsync()
	{
		if (_draggedSong == null) return;

		if (!PlaybackQueue.Contains(_draggedSong))
			PlaybackQueue.Add(_draggedSong);

		_draggedSong = null;
		System.Diagnostics.Debug.WriteLine("Drop completed");
		await Task.CompletedTask;
	}

	private async Task LoadMusicLibraryAsync()
	{
		var songsFromDb = await _databaseService.GetSongsAsync();

		if (songsFromDb.Count == 0)
		{
			await _databaseService.SaveSongAsync(new Song { Title = "Epic Beats", Artist = "DJ MAUI", FileName = "epic.mp3", IsFavorite = false });
			await _databaseService.SaveSongAsync(new Song { Title = "Lo-Fi Study", Artist = "Chill Coder", FileName = "lofi.mp3", IsFavorite = false });
			await _databaseService.SaveSongAsync(new Song { Title = "Kui rebeneb taevas", Artist = "Metsatöll", FileName = "Metsatoll-Kui_rebeneb_taevas.mp3", IsFavorite = false });
			await _databaseService.SaveSongAsync(new Song { Title = "Nothing Else Matters", Artist = "Metallica", FileName = "Metallica - Nothing Else Matters - Remastered 2021.mp3", IsFavorite = false });
			songsFromDb = await _databaseService.GetSongsAsync();
		}

		AllSongs.Clear();
		foreach (var song in songsFromDb)
			AllSongs.Add(song);
	}

	private async Task ScanDeviceAsync()
	{
		if (IsBusy) return;
		IsBusy = true;
		StatusMessage = "Skaneeritakse seadme kaustu...";

		try
		{
			// 1. Receive the tuple data containing both Path and Title
			var files = await _audioScanner.ScanAsync();
			if (files == null || files.Count == 0)
			{
				StatusMessage = "Ühtegi laulu ei leitud.";
				return;
			}

			var existing = await _databaseService.GetSongsAsync();
			int addedCount = 0;

			// 2. Loop through the paired data structures
			foreach (var item in files)
			{
				// Skip if this exact URI path is already stored in the DB
				if (existing.Exists(s => s.FileName == item.Path))
					continue;

				// 💡 Use the true media title fetched from the Android MediaStore query block
				var song = new Song
				{
					Title = string.IsNullOrWhiteSpace(item.Title) ? "Tundmatu lugu" : item.Title,
					Artist = "Unknown",
					FileName = item.Path, // Playable content:// path string
					IsFavorite = false,
					IsHidden = false
				};

				await _databaseService.SaveSongAsync(song);
				addedCount++;
			}

			await LoadMusicLibraryAsync();
			StatusMessage = $"Skaneerimine lõpetatud! Lisati {addedCount} uut laulu.";
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ScanDeviceAsync error: {ex}");
			StatusMessage = "Tõrge seadme skaneerimisel.";
		}
		finally
		{
			await Task.Delay(2500);
			IsBusy = false;
		}
	}




	private async Task PickFileAsync()
	{
		if (IsBusy) return;
		IsBusy = true;
		StatusMessage = "Avatakse failihaldurit...";

		try
		{
			var audioTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
		{
			{ DevicePlatform.Android, new[] { "audio/mpeg", "audio/mp3", "audio/x-mpeg" } }
		});

			var result = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Vali muusikafail",
				FileTypes = audioTypes
			});

			if (result == null)
			{
				StatusMessage = string.Empty;
				return;
			}

			// 1. Create a permanent directory inside your app's isolated secure container
			string userMusicFolder = Path.Combine(FileSystem.AppDataDirectory, "UserMusic");
			if (!Directory.Exists(userMusicFolder))
			{
				Directory.CreateDirectory(userMusicFolder);
			}

			// 2. Combine the folder path with the clean file name
			string localDestinationPath = Path.Combine(userMusicFolder, result.FileName);

			// 3. Copy the stream to prevent "revoked access token" crashes when switching tabs
			using (var sourceStream = await result.OpenReadAsync())
			using (var targetStream = File.Create(localDestinationPath))
			{
				await sourceStream.CopyToAsync(targetStream);
			}

			var existing = await _databaseService.GetSongsAsync();
			if (existing.Exists(s => s.FileName == localDestinationPath))
			{
				StatusMessage = "See lugu on juba kogus olemas.";
				return;
			}

			var song = new Song
			{
				Title = Path.GetFileNameWithoutExtension(result.FileName) ?? "Tundmatu lugu",
				Artist = "Unknown",
				FileName = localDestinationPath, // Now it points to a stable local file path
				IsFavorite = false,
				IsHidden = false
			};

			await _databaseService.SaveSongAsync(song);
			await LoadMusicLibraryAsync();
			StatusMessage = $"Lisatud: {song.Title}";
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"PickFileAsync viga: {ex}");
			StatusMessage = "Faili laadimine ebaõnnestus.";
		}
		finally
		{
			await Task.Delay(2000);
			IsBusy = false;
		}
	}

	public Task ScanDeviceAsyncPublic()
	{
		return ScanDeviceAsync();
	}

	private async Task HideSongAsync(Song? song)
	{
		if (song == null) return;
		song.IsHidden = true;
		await _databaseService.SaveSongAsync(song);
		AllSongs.Remove(song);
	}

	private async Task PlayPauseAsync()
	{
		if (CurrentSong == null)
		{
			if (PlaybackQueue.Count > 0)
				CurrentSong = PlaybackQueue[0];
			else if (AllSongs.Count > 0)
				CurrentSong = AllSongs[0];
		}

		if (_audioService.IsPlaying)
		{
			_audioService.Pause();
			IsPlaying = false;
		}
		else
		{
			if (CurrentSong != null)
			{
				await _audioService.PlayAsync(CurrentSong.FileName);
				IsPlaying = _audioService.IsPlaying;
			}
		}
	}

	private async Task PlaySongAsync(Song? song)
	{
		if (song == null) return; CurrentSong = song; if (!PlaybackQueue.Contains(song)) PlaybackQueue.Add(song); await _audioService.PlayAsync(song.FileName); IsPlaying = _audioService.IsPlaying; await Task.CompletedTask;
	}
	private async Task ToggleFavoriteAsync(Song? song) { if (song == null) return; song.IsFavorite = !song.IsFavorite; await _databaseService.SaveSongAsync(song); await LoadMusicLibraryAsync(); }
	protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
