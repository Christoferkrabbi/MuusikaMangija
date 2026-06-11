using SQLite;
using MuusikaMangija.Models;

namespace MuusikaMangija.Services;

public class DatabaseService
{
	private SQLiteAsyncConnection _database;

	private async Task InitAsync()
	{
		if (_database != null) return;

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MusicManager.db3");
		_database = new SQLiteAsyncConnection(dbPath);
		await _database.CreateTableAsync<Song>();
	}

	// Import any user-provided audio files from the AppData/UserMusic folder into the songs table.
	public async Task ImportUserSongsAsync()
	{
		await InitAsync();

		var userFolder = Path.Combine(FileSystem.AppDataDirectory, "UserMusic");
		if (!Directory.Exists(userFolder))
			Directory.CreateDirectory(userFolder);

		var files = Directory.GetFiles(userFolder, "*.mp3");
		foreach (var f in files)
		{
			// check if already exists (match by full path)
			var exists = await _database.Table<Song>().Where(s => s.FileName == f).FirstOrDefaultAsync();
			if (exists == null)
			{
				var song = new Song
				{
					Title = Path.GetFileNameWithoutExtension(f),
					Artist = "Unknown",
					FileName = f,
					IsFavorite = false
				};
				await _database.InsertAsync(song);
			}
		}
	}

	public async Task<List<Song>> GetSongsAsync()
	{
		await InitAsync();
        // Return only songs that are not hidden
		return await _database.Table<Song>().Where(s => s.IsHidden == false).ToListAsync();
	}

	public async Task<int> SaveSongAsync(Song song)
	{
		await InitAsync();
		if (song.Id != 0)
			return await _database.UpdateAsync(song);
		else
			return await _database.InsertAsync(song);
	}

	public async Task<int> DeleteSongAsync(Song song)
	{
		await InitAsync();
		return await _database.DeleteAsync(song);
	}

	public async Task<List<Song>> GetHiddenSongsAsync()
	{
		await InitAsync();
		return await _database.Table<Song>().Where(s => s.IsHidden == true).ToListAsync();
	}
}
