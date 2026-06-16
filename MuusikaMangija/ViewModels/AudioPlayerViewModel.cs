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
using System.Collections.Specialized;

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
	private int _currentQueueIndex = -1;

	private Song? _nextSong;
public Song? NextSong
{
	get => _nextSong;
	set
	{
		if (_nextSong == value) return;
		_nextSong = value;
		OnPropertyChanged(nameof(NextSong));
	}
}

private void PlaybackQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
{
	UpdateNextSong();
}

private void UpdateNextSong()
{
	if (_currentQueueIndex >= 0 && _currentQueueIndex + 1 < PlaybackQueue.Count)
	{
		NextSong = PlaybackQueue[_currentQueueIndex + 1];
	}
	else if (PlaybackQueue.Count > 0)
	{
		// if nothing playing, next is first item
		NextSong = PlaybackQueue[0];
	}
	else
	{
		NextSong = null;
	}
}
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
  public ICommand PlayPreviousCommand { get; }
	public ICommand PlayNextCommand { get; }
	public ICommand ToggleFavoriteCommand { get; }
	public ICommand DragStartingCommand { get; }
	public ICommand DropCommand { get; }
	public ICommand ScanDeviceCommand { get; }
	public ICommand HideSongCommand { get; }
    public ICommand AddToQueueCommand { get; }
	public ICommand RemoveFromQueueCommand { get; }
	public ICommand ClearQueueCommand { get; }
	public ICommand PickFileCommand { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public AudioPlayerViewModel(DatabaseService databaseService, IAudioScanner audioScanner, AudioService audioService)
	{
		_databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
		_audioScanner = audioScanner ?? throw new ArgumentNullException(nameof(audioScanner));
		_audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

		PlayPauseCommand = new Command(async () => await PlayPauseAsync());
		PlaySongCommand = new Command<Song>(async (s) => await PlaySongAsync(s));
       PlayPreviousCommand = new Command(async () => await PlayPreviousAsync());
		PlayNextCommand = new Command(async () => await PlayNextAsync());
		ToggleFavoriteCommand = new Command<Song>(async (s) => await ToggleFavoriteAsync(s));
		DragStartingCommand = new Command<Song>(OnDragStarting);
		DropCommand = new Command(async () => await OnDropAsync());
		ScanDeviceCommand = new Command(async () => await ScanDeviceAsync());
		HideSongCommand = new Command<Song>(async (s) => await HideSongAsync(s));
        PickFileCommand = new Command(async () => await PickFileAsync());
		AddToQueueCommand = new Command<Song>(song =>
		{
			if (song == null) return;
			if (!PlaybackQueue.Contains(song))
				PlaybackQueue.Add(song);
		});
        RemoveFromQueueCommand = new Command<Song>(async (song) => await RemoveFromQueueAsync(song));
		ClearQueueCommand = new Command(async () => await ClearQueueAsync());

		// update next song when queue changes
		PlaybackQueue.CollectionChanged += PlaybackQueue_CollectionChanged;

		UpdateNextSong();

		// subscribe to audio service playback end
		_audioService.PlaybackEnded += AudioService_PlaybackEnded;
	}

	private void AudioService_PlaybackEnded(object? sender, EventArgs e)
	{
		// run on main thread to update UI
		MainThread.BeginInvokeOnMainThread(async () =>
		{
             try
				{
					// advance to next in queue
					if (_currentQueueIndex + 1 < PlaybackQueue.Count)
					{
						var next = PlaybackQueue[_currentQueueIndex + 1];
						await PlaySongAsync(next);
						// update NextSong after starting next
						UpdateNextSong();
					}
					else
					{
						// no more songs
						IsPlaying = false;
						NextSong = null;
					}
				}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"AudioService_PlaybackEnded error: {ex}");
			}
		});
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
			
			await _databaseService.SaveSongAsync(new Song { Title = "Kui rebeneb taevas", Artist = "Metsatöll", FileName = "Metsatoll-Kui_rebeneb_taevas.mp3", IsFavorite = false });
			await _databaseService.SaveSongAsync(new Song { Title = "Nothing Else Matters", Artist = "Metallica", FileName = "Metallica - Nothing Else Matters - Remastered 2021.mp3", IsFavorite = false });
			await _databaseService.SaveSongAsync(new Song { Title = "Metallica - The Unforgiven - From James' Riff Tapes II", Artist = "Metallica", FileName = "Metallica - The Unforgiven - From James' Riff Tapes II.mp3", IsFavorite = false });
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
            // 1. Receive the tuple data containing Path, Title and Artist
			var files = await _audioScanner.ScanAsync();
			if (files == null || files.Count == 0)
			{
				StatusMessage = "Ühtegi laulu ei leitud.";
				return;
			}

			var existing = await _databaseService.GetSongsAsync();
			int addedCount = 0;

            // 2. Loop through the returned tuples
			foreach (var item in files)
			{
				// item is (Path, Title, Artist)
				var path = item.Path;
				var title = item.Title;
				var artist = item.Artist;

				// Skip if this exact URI path is already stored in the DB
				if (existing.Exists(s => s.FileName == path))
					continue;

				var song = new Song
				{
					Title = string.IsNullOrWhiteSpace(title) ? "Tundmatu lugu" : title,
					Artist = string.IsNullOrWhiteSpace(artist) ? "Unknown" : artist,
					FileName = path, // Playable content:// path string or local path
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

			// --- NEW: Read ID3 Metadata using TagLibSharp ---
			string parsedTitle = string.Empty;
			string parsedArtist = string.Empty;

			try
			{
				using (var tfile = TagLib.File.Create(localDestinationPath))
				{
					parsedTitle = tfile.Tag.Title;
					// FirstArtists extracts the first primary artist from the performers array
					parsedArtist = tfile.Tag.FirstArtist;
				}
			}
			catch (Exception tagEx)
			{
				System.Diagnostics.Debug.WriteLine($"Metadata read fallback: {tagEx.Message}");
			}

			// Fallback to filename if metadata tags are empty or missing
			if (string.IsNullOrWhiteSpace(parsedTitle))
			{
				parsedTitle = Path.GetFileNameWithoutExtension(result.FileName) ?? "Tundmatu lugu";
			}
			if (string.IsNullOrWhiteSpace(parsedArtist))
			{
				parsedArtist = "Unknown";
			}
			// ------------------------------------------------

			var song = new Song
			{
				Title = parsedTitle,
				Artist = parsedArtist,
				FileName = localDestinationPath, // Points to a stable local file path
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
				// set UI to playing immediately for responsive feedback
                IsPlaying = true;
				await _audioService.PlayAsync(CurrentSong.FileName);
				IsPlaying = _audioService.IsPlaying;
			}
		}
	}

	private async Task PlaySongAsync(Song? song)
	{
		if (song == null)
			return;

		CurrentSong = song;

		if (!PlaybackQueue.Contains(song))
			PlaybackQueue.Add(song);

		_currentQueueIndex = PlaybackQueue.IndexOf(song);

		await _audioService.PlayAsync(song.FileName);
		IsPlaying = _audioService.IsPlaying;
		UpdateNextSong();
	}

	private async Task RemoveFromQueueAsync(Song? song)
	{
		if (song == null) return;
		if (PlaybackQueue.Contains(song))
			PlaybackQueue.Remove(song);
		// If removed song was the current one, clear CurrentSong
		if (CurrentSong == song)
		{
			CurrentSong = null;
			_audioService.Stop();
			IsPlaying = false;
		}
		await Task.CompletedTask;
	}

	private async Task ClearQueueAsync()
	{
		PlaybackQueue.Clear();
		CurrentSong = null;
		_audioService.Stop();
		IsPlaying = false;
		await Task.CompletedTask;
	}

	private async Task PlayNextAsync()
	{
		if (PlaybackQueue.Count == 0) return;
		int nextIndex = _currentQueueIndex + 1;
		if (nextIndex >= PlaybackQueue.Count) return;
		await PlaySongAsync(PlaybackQueue[nextIndex]);
	}

	private async Task PlayPreviousAsync()
	{
		if (PlaybackQueue.Count == 0) return;
		int prevIndex = _currentQueueIndex - 1;
		if (prevIndex < 0) return;
		await PlaySongAsync(PlaybackQueue[prevIndex]);
	}

	private async Task ToggleFavoriteAsync(Song? song)
	{
		if (song == null) return;
		song.IsFavorite = !song.IsFavorite;
		await _databaseService.SaveSongAsync(song);
		await LoadMusicLibraryAsync();
	}
	protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
