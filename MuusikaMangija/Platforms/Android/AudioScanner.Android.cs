using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Provider;
using Android.Database;
using Android.Content;
using MuusikaMangija.Services;

namespace MuusikaMangija.Services
{
    // Android-specific scanner that queries MediaStore for audio files
    public class AndroidAudioScanner : IAudioScanner
    {
        public Task<List<string>> ScanAsync()
        {
            var list = new List<string>();
            try
            {
                var uri = MediaStore.Audio.Media.ExternalContentUri;
                string[] projection = new[] { MediaStore.Audio.AudioColumns.Data };
                var resolver = Android.App.Application.Context.ContentResolver;
                using (ICursor cursor = resolver.Query(uri, projection, null, null, null))
                {
                    if (cursor != null)
                    {
                        int dataIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Data);
                        while (cursor.MoveToNext())
                        {
                            var path = cursor.GetString(dataIndex);
                            if (!string.IsNullOrWhiteSpace(path))
                                list.Add(path);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidAudioScanner error: {ex}");
            }

            return Task.FromResult(list);
        }
    }
}
