using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MuusikaMangija.Models;
using MuusikaMangija.Services;

namespace MuusikaMangija.ViewModels;

public class AudioPlayerViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private readonly IAudioScanner _audioScanner;

    private Song? _currentSong;
    private bool _isPlaying;
    private Song? _draggedSong;

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

    public ICommand PlayPauseCommand { get; }
    public ICommand PlaySongCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand DragStartingCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand ScanDeviceCommand { get; }
    public ICommand HideSongCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AudioPlayerViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

        PlayPauseCommand = new Command(async () => await PlayPauseAsync());
        PlaySongCommand = new Command<Song>(async (s) => await PlaySongAsync(s));
        ToggleFavoriteCommand = new Command<Song>(async (s) => await ToggleFavoriteAsync(s));
        DragStartingCommand = new Command<Song>(OnDragStarting);
        DropCommand = new Command(async () => await OnDropAsync());

        // Do not perform heavy async work in the constructor. Call InitializeAsync from the page's OnAppearing.
    }

    // Initialize asynchronously (call from page OnAppearing)
    public Task InitializeAsync()
    {
        // Import any user-added songs from AppData/UserMusic before loading the library
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

        // add to playback queue (do not duplicate if already in queue)
        if (!PlaybackQueue.Contains(_draggedSong))
            PlaybackQueue.Add(_draggedSong);

        _draggedSong = null;
        System.Diagnostics.Debug.WriteLine("Drop completed");
        await Task.CompletedTask;
    }

    private async Task LoadMusicLibraryAsync()
    {
        var songsFromDb = await _databaseService.GetSongsAsync();

        // Seed sample data if DB is empty
        if (songsFromDb.Count == 0)
        {
            await _databaseService.SaveSongAsync(new Song { Title = "Epic Beats", Artist = "DJ MAUI", FileName = "epic.mp3", IsFavorite = false });
            await _databaseService.SaveSongAsync(new Song { Title = "Lo-Fi Study", Artist = "Chill Coder", FileName = "lofi.mp3", IsFavorite = false });
            await _databaseService.SaveSongAsync(new Song { Title = "Kui rebeneb taevas", Artist = "Metsatöll", FileName = "Metsatoll-Kui_rebeneb_taevas", IsFavorite = false });
            songsFromDb = await _databaseService.GetSongsAsync();
        }

        AllSongs.Clear();
        foreach (var song in songsFromDb)
            AllSongs.Add(song);
    }

    private async Task ScanDeviceAsync()
    {
        try
        {
            var files = await _audioScanner.ScanAsync();
            if (files == null || files.Count == 0)
                return;

            var existing = await _databaseService.GetSongsAsync();
            foreach (var path in files)
            {
                // skip if already present by exact path
                if (existing.Exists(s => s.FileName == path))
                    continue;

                var song = new Song
                {
                    Title = Path.GetFileNameWithoutExtension(path),
                    Artist = "Unknown",
                    FileName = path,
                    IsFavorite = false,
                    IsHidden = false
                };
                await _databaseService.SaveSongAsync(song);
            }

            await LoadMusicLibraryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScanDeviceAsync error: {ex}");
        }
    }

    private async Task HideSongAsync(Song? song)
    {
        if (song == null) return;
        song.IsHidden = true;
        await _databaseService.SaveSongAsync(song);

        // remove from UI collection
        AllSongs.Remove(song);
    }

    private async Task PlayPauseAsync()
    {
        if (CurrentSong == null)
        {
            // try to pick first from queue
            if (PlaybackQueue.Count > 0)
                CurrentSong = PlaybackQueue[0];
            else if (AllSongs.Count > 0)
                CurrentSong = AllSongs[0];
        }

        IsPlaying = !IsPlaying;

        await Task.CompletedTask; // placeholder for integrating actual audio playback
    }

    private async Task PlaySongAsync(Song? song)
    {
        if (song == null) return;
        CurrentSong = song;
        if (!PlaybackQueue.Contains(song))
            PlaybackQueue.Add(song);

        IsPlaying = true;
        await Task.CompletedTask;
    }

    private async Task ToggleFavoriteAsync(Song? song)
    {
        if (song == null) return;

        song.IsFavorite = !song.IsFavorite;
        await _databaseService.SaveSongAsync(song);

        // refresh list so UI updates binding
        await LoadMusicLibraryAsync();
    }

    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}