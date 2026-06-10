using System.Collections.Generic;
using System.Threading.Tasks;

namespace MuusikaMangija.Services;

public interface IAudioScanner
{
    // Returns full file paths of audio files found on the device
    Task<List<string>> ScanAsync();
}
