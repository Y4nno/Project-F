using Project_F;
using Project_F.Services;
using System.Windows.Input;

public class LoginViewModel
{
    private readonly FirebaseAuthService _authService = new();

    public string Email { get; set; }
    public string Password { get; set; }

    public ICommand LoginCommand => new Command(async () => await Login());
    public ICommand GoToRegisterCommand => new Command(async () =>
        await Shell.Current.GoToAsync("//RegisterPage"));

    private async Task Login()
    {
        var (success, token, uid, error) =
            await _authService.SignInAsync(Email, Password);

        if (!success)
        {
            await Application.Current.MainPage.DisplayAlert("Error", error, "OK");
            return;
        }

        Preferences.Set("idToken", token);
        Preferences.Set("uid", uid);

        string? displayName = await _authService.GetUserNameAsync(uid!, token!);
        string username = displayName ?? Email.Split('@')[0];

        Preferences.Set("user_display_name", username);
        App.UserId = username; // ✅ now stores username, not uid

        await Shell.Current.GoToAsync("//HomePage");
    }
}