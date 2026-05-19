using System.Windows.Input;
using Project_F.Services;

public class RegisterViewModel
{
    private readonly FirebaseAuthService _authService = new();

    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public ICommand RegisterCommand => new Command(async () => await Register());
    public ICommand BackCommand => new Command(async () =>
        await Shell.Current.GoToAsync("//LoginPage"));

    private async Task Register()
    {
        var (success, error) =
            await _authService.SignUpAsync(Name, Email, Password);

        if (!success)
        {
            await Application.Current.MainPage.DisplayAlert("Error", error, "OK");
            return;
        }

        await Application.Current.MainPage.DisplayAlert("Success", "Account created!", "OK");

        await Shell.Current.GoToAsync("//LoginPage");
    }
}