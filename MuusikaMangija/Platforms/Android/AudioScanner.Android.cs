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
		public Task<List<(string Path, string Title)>> ScanAsync()
		{
			var list = new List<(string Path, string Title)>();
			try
			{
				var uri = MediaStore.Audio.Media.ExternalContentUri;

				// 💡 Query BOTH the ID and the real Media Title string from the phone
				string[] projection = new[]
				{
					MediaStore.Audio.AudioColumns.Id,
					MediaStore.Audio.AudioColumns.Title
				};

				var resolver = Android.App.Application.Context.ContentResolver;
				using (ICursor cursor = resolver.Query(uri, projection, null, null, null))
				{
					if (cursor != null)
					{
						int idIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Id);
						int titleIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.AudioColumns.Title);

						while (cursor.MoveToNext())
						{
							long id = cursor.GetLong(idIndex);
							string realTitle = cursor.GetString(titleIndex);

							// Generate the valid content URI string
							var contentUri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, id);

							if (contentUri != null && !string.IsNullOrWhiteSpace(realTitle))
							{
								// Return the playable URI path paired with its real metadata title
								list.Add((contentUri.ToString(), realTitle));
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
