using System;
using System.IO;
using System.Threading.Tasks;
using Plugin.Maui.Audio;
using Microsoft.Maui.Storage;

namespace MuusikaMangija.Services;

public class AudioService : IDisposable
{
    private IAudioPlayer? _player;
    private Stream? _stream;
    private string? _currentFilePath;
    private string? _tempCopiedPath;
    private bool _pauseRequested = false;
    private bool _stopRequested = false;
    private System.Threading.CancellationTokenSource? _monitorCts;

    // Raised when playback naturally finishes
    public event EventHandler? PlaybackEnded;

    public bool IsPlaying => _player?.IsPlaying ?? false;

    // Play filePath. Supports absolute device paths or packaged app assets (Resources/Raw)
    public async Task PlayAsync(string filePath)
    {
        System.Diagnostics.Debug.WriteLine($"AudioService.PlayAsync called for: {filePath}");
        try
        {
            if (!string.IsNullOrEmpty(_currentFilePath) && _currentFilePath == filePath && _player != null && !_player.IsPlaying)
            {
                _player.Play();
                return;
            }
        }
        catch { }

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
                StartPlaybackMonitor();
                return;
            }

#if ANDROID
            // Handle Android content:// URIs returned by MediaStore scanner.
            // Some players cannot operate directly on a ContentResolver stream reliably, so copy to a temp file first.
            if (filePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = Android.Net.Uri.Parse(filePath);
                    var resolver = Android.App.Application.Context.ContentResolver;
                    using var istream = resolver.OpenInputStream(uri);
                    if (istream != null)
                    {
                        // copy with a unique name
                        var cacheDir = FileSystem.CacheDirectory;
                        var tmpName = $"audiotmp_{Guid.NewGuid():N}.tmp";
                        var tmpPath = Path.Combine(cacheDir, tmpName);
                        using (var outStream = File.Create(tmpPath))
                        {
                            await istream.CopyToAsync(outStream);
                        }

                        // open the temp file for playback
                        _stream = File.OpenRead(tmpPath);
                        _player = AudioManager.Current.CreatePlayer(_stream);
                        _player.Play();
                        _currentFilePath = filePath; // original content URI
                        _tempCopiedPath = tmpPath; // temp copy we should clean up on Stop
                        StartPlaybackMonitor();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AudioService: failed to open/copy content URI stream: {ex}");
                }
            }
#endif

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
                        StartPlaybackMonitor();
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
                _pauseRequested = true;
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
            _stopRequested = true;
            _monitorCts?.Cancel();

            _player?.Stop();
            _player?.Dispose();
            _player = null;
            _stream?.Dispose();
            _stream = null;
            _currentFilePath = null;
            try
            {
                if (!string.IsNullOrEmpty(_tempCopiedPath) && File.Exists(_tempCopiedPath))
                {
                    File.Delete(_tempCopiedPath);
                }
            }
            catch { }
            _tempCopiedPath = null;
        }
        catch { }
    }

    private void StartPlaybackMonitor()
    {
        try
        {
            _monitorCts?.Cancel();
            _monitorCts = new System.Threading.CancellationTokenSource();
            var ct = _monitorCts.Token;
            _pauseRequested = false;
            _stopRequested = false;

            // Poll player state in background to detect when it stops naturally
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested && _player != null)
                    {
                        await System.Threading.Tasks.Task.Delay(500, ct);
                        if (ct.IsCancellationRequested) break;

                        // If player exists but is not playing and user did not request pause/stop -> end of playback
                        if (_player != null && !_player.IsPlaying && !_pauseRequested && !_stopRequested)
                        {
                            System.Diagnostics.Debug.WriteLine("AudioService: detected playback ended naturally");
                            PlaybackEnded?.Invoke(this, EventArgs.Empty);
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Playback monitor error: {ex}");
                }
            }, ct);
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
