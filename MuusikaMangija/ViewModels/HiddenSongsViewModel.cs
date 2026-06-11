using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using MuusikaMangija.Models;
using MuusikaMangija.Services;
using Microsoft.Maui.Controls;

namespace MuusikaMangija.ViewModels;

public class HiddenSongsViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<Song> HiddenSongs { get; } = new();

    public ICommand UnhideCommand { get; }
    public ICommand DeleteCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public HiddenSongsViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

        UnhideCommand = new Command<Song>(async (s) => await UnhideAsync(s));
        DeleteCommand = new Command<Song>(async (s) => await DeleteAsync(s));
    }

    public async Task InitializeAsync()
    {
        await LoadHiddenAsync();
    }

    private async Task LoadHiddenAsync()
    {
        HiddenSongs.Clear();
        var list = await _databaseService.GetHiddenSongsAsync();
        foreach (var s in list)
            HiddenSongs.Add(s);
    }

    private async Task UnhideAsync(Song? song)
    {
        if (song == null) return;
        song.IsHidden = false;
        await _databaseService.SaveSongAsync(song);
        HiddenSongs.Remove(song);
    }

    private async Task DeleteAsync(Song? song)
    {
        if (song == null) return;
        await _databaseService.DeleteSongAsync(song);
        HiddenSongs.Remove(song);
    }
}
