using System.ComponentModel;
using System.Globalization;
using MuusikaMangija.Resources.Localization;

namespace MuusikaMangija.Services;

public class LocalizationManager : INotifyPropertyChanged
{
	public static LocalizationManager Instance { get; } = new();

	public string this[string key] => AppResources.ResourceManager.GetString(key, CurrentCulture) ?? $"?{key}?";

	private CultureInfo _currentCulture = CultureInfo.CurrentCulture;
	public CultureInfo CurrentCulture
	{
		get => _currentCulture;
		set
		{
			if (_currentCulture != value)
			{
				_currentCulture = value;
				CultureInfo.DefaultThreadCurrentCulture = value;
				CultureInfo.DefaultThreadCurrentUICulture = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}
