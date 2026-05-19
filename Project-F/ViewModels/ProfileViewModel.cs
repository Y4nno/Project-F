using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Project_F.ViewModels;

public class ProfileViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _displayName = "Loading…";
    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); }
    }

    public ICommand SwitchAccountCommand { get; }
    public ICommand LogOutCommand { get; }
    public ICommand GoHomeCommand { get; }
    public ICommand GoCalendarCommand { get; }

    public ProfileViewModel()
    {
        SwitchAccountCommand = new Command(OnSwitchAccount);
        LogOutCommand = new Command(async () => await OnLogOut());
        GoHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
        GoCalendarCommand = new Command(async () => await Shell.Current.GoToAsync("//CalendarPage"));

        LoadUserInfo();
    }

    public void LoadUserInfo()
    {
        string name = Preferences.Get("user_display_name", "");
        DisplayName = !string.IsNullOrWhiteSpace(name) ? name : "User";
    }

    private async void OnSwitchAccount()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Switch Account", "Sign out and switch to another account?", "Yes", "Cancel");

        if (confirm) await SignOutAndNavigate();
    }

    private async Task OnLogOut()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Log Out", "Are you sure you want to log out?", "Log Out", "Cancel");

        if (confirm) await SignOutAndNavigate();
    }

    private async Task SignOutAndNavigate()
    {
        Preferences.Remove("user_display_name");
        Preferences.Remove("user_email");
        Preferences.Remove("idToken");
        Preferences.Remove("uid");

        await Shell.Current.GoToAsync("//LoginPage");
    }
}