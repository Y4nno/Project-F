namespace Project_F;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
        Routing.RegisterRoute(nameof(Pages.RegisterPage), typeof(Pages.RegisterPage));
        Routing.RegisterRoute(nameof(Pages.HomePage), typeof(Pages.HomePage));
        Routing.RegisterRoute(nameof(Pages.CalendarPage), typeof(Pages.CalendarPage));
        Routing.RegisterRoute(nameof(Pages.ProfilePage), typeof(Pages.ProfilePage));
    }
}