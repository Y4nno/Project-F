using Newtonsoft.Json;
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

    private string _totalBalance = "₱0.00";
    public string TotalBalance
    {
        get => _totalBalance;
        set { _totalBalance = value; OnPropertyChanged(); }
    }

    private string _totalIncome = "₱0.00";
    public string TotalIncome
    {
        get => _totalIncome;
        set { _totalIncome = value; OnPropertyChanged(); }
    }

    private string _totalExpenses = "₱0.00";
    public string TotalExpenses
    {
        get => _totalExpenses;
        set { _totalExpenses = value; OnPropertyChanged(); }
    }

    private string _transactionCount = "0";
    public string TransactionCount
    {
        get => _transactionCount;
        set { _transactionCount = value; OnPropertyChanged(); }
    }

    private string _balanceColor = "#4CAF50";
    public string BalanceColor
    {
        get => _balanceColor;
        set { _balanceColor = value; OnPropertyChanged(); }
    }

    private string _avatarInitial = "U";
    public string AvatarInitial
    {
        get => _avatarInitial;
        set { _avatarInitial = value; OnPropertyChanged(); }
    }

    public ICommand SwitchAccountCommand { get; }
    public ICommand LogOutCommand { get; }

    public ProfileViewModel()
    {
        SwitchAccountCommand = new Command(OnSwitchAccount);
        LogOutCommand = new Command(async () => await OnLogOut());
    }

    public void LoadUserInfo()
    {
        string name = Preferences.Get("user_display_name", "");
        DisplayName = !string.IsNullOrWhiteSpace(name) ? name : "User";
        AvatarInitial = DisplayName.Length > 0 ? DisplayName[0].ToString().ToUpper() : "U";
    }

    public async Task LoadStats()
    {
        using var client = new HttpClient();
        var idToken = Preferences.Get("idToken", null);
        if (string.IsNullOrEmpty(idToken)) return;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        string url = "https://firestore.googleapis.com/v1/projects/project-f-c6e4e/databases/(default)/documents/transactions";

        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);
            if (data?.documents == null) return;

            string userId = App.UserId;
            double income = 0, expenses = 0;
            int count = 0;

            foreach (var doc in data.documents)
            {
                string uid = doc.fields.UserId.stringValue;
                if (uid != userId) continue;

                double amount = 0;
                if (doc.fields.Amount.doubleValue != null)
                    amount = (double)doc.fields.Amount.doubleValue;
                else if (doc.fields.Amount.integerValue != null)
                    amount = (double)doc.fields.Amount.integerValue;

                string type = doc.fields.Type.stringValue;
                if (type == "Income") income += amount;
                else expenses += amount;

                count++;
            }

            double balance = income - expenses;

            TotalIncome = $"₱{income:F2}";
            TotalExpenses = $"₱{expenses:F2}";
            TotalBalance = $"₱{Math.Abs(balance):F2}";
            BalanceColor = balance >= 0 ? "#4CAF50" : "#FF5252";
            TransactionCount = count.ToString();
        }
        catch { }
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
        App.UserId = null;
        await Shell.Current.GoToAsync("//LoginPage");
    }
}