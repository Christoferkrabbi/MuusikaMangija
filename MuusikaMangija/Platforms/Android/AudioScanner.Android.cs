using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Provider;
using Android.Database;
using Android.Content;

namespace MuusikaMangija.Services
{
	public class AndroidAudioScanner : IAudioScanner
	{
      public Task<List<(string Path, string Title, string Artist)>> ScanAsync()
		{
         var list = new List<(string Path, string Title, string Artist)>();
			try
			{
				var uri = MediaStore.Audio.Media.ExternalContentUri;

                 string[] projection = new[]
				{
					MediaStore.Audio.AudioColumns.Id,
                     MediaStore.Audio.AudioColumns.Title,
						MediaStore.Audio.AudioColumns.Artist
				};

				var resolver = Android.App.Application.Context.ContentResolver;
				using (ICursor cursor = resolver.Query(uri, projection, null, null, null))
				{
					if (cursor != null)
					{
                       int idIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Id);
						int titleIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Title);
						int artistIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Artist);

						while (cursor.MoveToNext())
						{
							long id = cursor.GetLong(idIndex);
                            string realTitle = cursor.GetString(titleIndex);
							string artist = string.Empty;
							try { artist = cursor.GetString(artistIndex); } catch { artist = ""; }

							var contentUri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, id);

                            if (contentUri != null && !string.IsNullOrWhiteSpace(realTitle))
						{
							// Return the playable URI path paired with its real metadata title and artist
							list.Add((contentUri.ToString(), realTitle, artist ?? string.Empty));
						}
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"AndroidAudioScanner error: {ex}");
			}

			return Task.FromResult(list);
		}
	}
}
