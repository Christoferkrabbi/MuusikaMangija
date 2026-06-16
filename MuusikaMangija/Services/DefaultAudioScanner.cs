using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MuusikaMangija.Services;

// Cross-platform fallback scanner that looks in AppData/UserMusic
public class DefaultAudioScanner : IAudioScanner
{
    public Task<List<(string Path, string Title, string Artist)>> ScanAsync()
	{
        // 💡 Update collection to use the tuple structure matching your new interface
		var list = new List<(string Path, string Title, string Artist)>();

		var userFolder = Path.Combine(FileSystem.AppDataDirectory, "UserMusic");
		if (!Directory.Exists(userFolder))
			Directory.CreateDirectory(userFolder);

		var files = Directory.GetFiles(userFolder, "*.mp3");

		foreach (var file in files)
		{
           // Extract a clean song title from the file name text on disk
			string cleanTitle = Path.GetFileNameWithoutExtension(file);
			string artist = "Unknown";

			if (string.IsNullOrWhiteSpace(cleanTitle))
				cleanTitle = "Tundmatu lugu";

			// Optionally split titles like "Artist - Title"
			if (cleanTitle.Contains(" - "))
			{
				var parts = cleanTitle.Split(new[] { " - " }, 2, System.StringSplitOptions.None);
				if (parts.Length == 2)
				{
					artist = parts[0].Trim();
					cleanTitle = parts[1].Trim();
				}
			}

			list.Add((file, cleanTitle, artist));
		}

		return Task.FromResult(list);
	}
}
