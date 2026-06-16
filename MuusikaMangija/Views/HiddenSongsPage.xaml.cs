using MuusikaMangija.ViewModels;

namespace MuusikaMangija.Views;

public partial class HiddenSongsPage : ContentPage
{
    private readonly HiddenSongsViewModel _vm;
    public HiddenSongsPage(HiddenSongsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button b && b.CommandParameter is MuusikaMangija.Models.Song song)
            {
                var ok = await DisplayAlert("Confirm delete", $"Permanently delete '{song.Title}'? This will remove the file from device.", "Delete", "Cancel");
                if (!ok) return;

                await _vm.DeleteSongAndFileAsync(song);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDeleteClicked error: {ex}");
            await DisplayAlert("Error", "Could not delete file.", "OK");
        }
    }
}
