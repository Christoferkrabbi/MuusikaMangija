using Microsoft.Maui.Hosting;
using MuusikaMangija.Services;

namespace MuusikaMangija
{
    public static partial class MauiProgram
    {
        static partial void RegisterPlatformServices(MauiAppBuilder builder)
        {
            // Register Android-specific audio scanner implementation
            builder.Services.AddSingleton<IAudioScanner, AndroidAudioScanner>();
        }
    }
}
