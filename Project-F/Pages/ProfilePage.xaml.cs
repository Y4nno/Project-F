using Project_F.ViewModels;

namespace Project_F.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ProfileViewModel vm)
        {
            vm.LoadUserInfo();
            await vm.LoadStats();
        }
    }

    async void OnHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    async void OnCalendarClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//CalendarPage");

    async void OnProfileClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//ProfilePage");
}