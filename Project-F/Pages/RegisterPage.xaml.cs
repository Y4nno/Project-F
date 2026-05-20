namespace Project_F.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
        BindingContext = new RegisterViewModel();
    }

    private void OnTogglePassword(object sender, TappedEventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordToggle.Text = PasswordEntry.IsPassword ? "SHOW" : "HIDE";
        PasswordToggle.TextColor = PasswordEntry.IsPassword
            ? Color.FromArgb("#888888")
            : Color.FromArgb("#FF9800");
    }

    private void OnToggleConfirmPassword(object sender, TappedEventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        ConfirmPasswordToggle.Text = ConfirmPasswordEntry.IsPassword ? "SHOW" : "HIDE";
        ConfirmPasswordToggle.TextColor = ConfirmPasswordEntry.IsPassword
            ? Color.FromArgb("#888888")
            : Color.FromArgb("#FF9800");
    }
}