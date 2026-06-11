using System;
using System.IO;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

namespace MuusikaMangija.Services;

public class AudioService : IDisposable
{
    private IAudioPlayer? _player;
    private Stream? _stream;
    private string? _currentFilePath;

    public bool IsPlaying => _player?.IsPlaying ?? false;

    // Play filePath. Supports absolute device paths or packaged app assets (Resources/Raw)
    public async Task PlayAsync(string filePath)
    {
        // If player already open for same file and is paused, resume instead of recreating
        try
        {
            if (!string.IsNullOrEmpty(_currentFilePath) && _currentFilePath == filePath && _player != null && !_player.IsPlaying)
            {
                _player.Play();
                return;
            }
        }
        catch { }

        // otherwise create new player
        Stop();

        try
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            // If it's an absolute path on device
            if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            {
                _stream = File.OpenRead(filePath);
                _player = AudioManager.Current.CreatePlayer(_stream);
                _player.Play();
                _currentFilePath = filePath;
                return;
            }

            // Try to open as packaged app asset via FileSystem (Resources/Raw)
            var filename = Path.GetFileName(filePath);
            if (!string.IsNullOrEmpty(filename))
            {
                try
                {
                    var s = await FileSystem.OpenAppPackageFileAsync(filename);
                    if (s != null)
                    {
                        _stream = s;
                        _player = AudioManager.Current.CreatePlayer(_stream);
                        _player.Play();
                        _currentFilePath = filename;
                        return;
                    }
                }
                catch (Exception ioe)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenAppPackageFileAsync failed: {ioe}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService Play error: {ex}");
            Stop();
        }
    }

    public void Pause()
    {
        try
        {
            if (_player?.IsPlaying == true)
            {
                _player.Pause();
            }
        }
        catch { }
    }

    public void Resume()
    {
        try
        {
            if (_player != null && !_player.IsPlaying)
                _player.Play();
        }
        catch { }
    }

    public void Stop()
    {
        try
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
            _stream?.Dispose();
            _stream = null;
            _currentFilePath = null;
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
