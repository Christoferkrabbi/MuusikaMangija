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

    // Delete DB record and remove the physical file if it exists
    public async Task DeleteSongAndFileAsync(Song? song)
    {
        if (song == null) return;

        try
        {
            var path = song.FileName;
            bool deleted = false;
            if (!string.IsNullOrEmpty(path))
            {
                // If it's a content URI (MediaStore), delete via ContentResolver
#if ANDROID
                try
                {
                    if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                    {
                        var resolver = Android.App.Application.Context.ContentResolver;
                        var contentUri = Android.Net.Uri.Parse(path);
                        try
                        {
                            var rows = resolver.Delete(contentUri, null, null);
                            System.Diagnostics.Debug.WriteLine($"MediaStore content URI delete rows: {rows}");
                            deleted = rows > 0;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete content URI: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Content URI delete attempt failed: {ex}");
                }
#endif

                // If not deleted yet and path is a rooted filesystem path, try File.Delete
                if (!deleted && System.IO.Path.IsPathRooted(path))
                {
                    try
                    {
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                            deleted = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete physical file via File.Delete: {ex}");
                    }
                }

#if ANDROID
                if (!deleted)
                {
                    try
                    {
                        var resolver = Android.App.Application.Context.ContentResolver;
                        var uri = Android.Provider.MediaStore.Audio.Media.ExternalContentUri;
                        string[] projection = new[] { Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Id };
                        string selection = Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Data + "=?";
                        string[] selectionArgs = new[] { path };
                        using (var cursor = resolver.Query(uri, projection, selection, selectionArgs, null))
                        {
                            if (cursor != null && cursor.MoveToFirst())
                            {
                                var idIndex = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Id);
                                var id = cursor.GetLong(idIndex);
                                var contentUri = Android.Content.ContentUris.WithAppendedId(Android.Provider.MediaStore.Audio.Media.ExternalContentUri, id);
                                var rows = resolver.Delete(contentUri, null, null);
                                System.Diagnostics.Debug.WriteLine($"MediaStore delete rows: {rows}");
                                deleted = rows > 0;
                            }
                        }
                    }
                    catch (Exception mex)
                    {
                        System.Diagnostics.Debug.WriteLine($"MediaStore deletion failed: {mex}");
                    }
                }
#endif
            }

            await _databaseService.DeleteSongAsync(song);
            HiddenSongs.Remove(song);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteSongAndFileAsync error: {ex}");
            throw;
        }
    }
}
