using System.Collections.Generic;
using System.Threading.Tasks;

namespace MuusikaMangija.Services
{
    public interface IAudioScanner
	{
		// Returns tuples of Path, Title and Artist
		Task<List<(string Path, string Title, string Artist)>> ScanAsync();
	}
}

