using System.Collections.Generic;
using System.Threading.Tasks;

namespace MuusikaMangija.Services
{
    public interface IAudioScanner
	{
		Task<List<(string Path, string Title, string Artist)>> ScanAsync();
	}
}

