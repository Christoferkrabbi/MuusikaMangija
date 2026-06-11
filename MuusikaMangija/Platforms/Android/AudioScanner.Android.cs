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
		public Task<List<string>> ScanAsync()
		{
			var list = new List<string>();
			try
			{
				var uri = MediaStore.Audio.Media.ExternalContentUri;

				// Query the ID instead of the raw file Path string to stop Android 11 crashes
				string[] projection = new[] { MediaStore.Audio.AudioColumns.Id };

				var resolver = Android.App.Application.Context.ContentResolver;
				using (ICursor cursor = resolver.Query(uri, projection, null, null, null))
				{
					if (cursor != null)
					{
						int idIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Id);
						while (cursor.MoveToNext())
						{
							long id = cursor.GetLong(idIndex);

							// Generate a standard playable Android Content URI string
							var contentUri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, id);

							if (contentUri != null)
								list.Add(contentUri.ToString());
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
