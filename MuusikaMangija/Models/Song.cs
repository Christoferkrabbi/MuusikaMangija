using SQLite;

namespace MuusikaMangija.Models;

public class Song
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public string Title { get; set; }
	public string Artist { get; set; }
	public string FileName { get; set; } // Faili nimi Resources/Raw/ kaustas
	public bool IsFavorite { get; set; }
    // If true, song is hidden from UI but kept in DB
	public bool IsHidden { get; set; }
}
