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
}
