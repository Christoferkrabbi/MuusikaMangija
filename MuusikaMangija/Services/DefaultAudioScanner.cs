using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MuusikaMangija.Services;

// Cross-platform fallback scanner that looks in AppData/UserMusic
public class DefaultAudioScanner : IAudioScanner
{
	public Task<List<(string Path, string Title)>> ScanAsync()
	{
		// 💡 Update collection to use the tuple structure matching your new interface
		var list = new List<(string Path, string Title)>();

		var userFolder = Path.Combine(FileSystem.AppDataDirectory, "UserMusic");
		if (!Directory.Exists(userFolder))
			Directory.CreateDirectory(userFolder);

		var files = Directory.GetFiles(userFolder, "*.mp3");

		foreach (var file in files)
		{
			// Extract a clean song title from the file name text on disk
			string cleanTitle = Path.GetFileNameWithoutExtension(file);

			if (string.IsNullOrWhiteSpace(cleanTitle))
				cleanTitle = "Tundmatu lugu";

			// Add both the file path and its clean title name to our list
			list.Add((file, cleanTitle));
		}

		return Task.FromResult(list);
	}
}
