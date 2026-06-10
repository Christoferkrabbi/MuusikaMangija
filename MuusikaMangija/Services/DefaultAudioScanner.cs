using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MuusikaMangija.Services;

// Cross-platform fallback scanner that looks in AppData/UserMusic
public class DefaultAudioScanner : IAudioScanner
{
    public Task<List<string>> ScanAsync()
    {
        var list = new List<string>();
        var userFolder = Path.Combine(FileSystem.AppDataDirectory, "UserMusic");
        if (!Directory.Exists(userFolder))
            Directory.CreateDirectory(userFolder);

        var files = Directory.GetFiles(userFolder, "*.mp3");
        list.AddRange(files);
        return Task.FromResult(list);
    }
}
