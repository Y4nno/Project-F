using Newtonsoft.Json;
using Project_F.Models;
using System.Collections.ObjectModel;
using System.Text;

namespace Project_F.Pages;

public partial class HomePage : ContentPage
{
    ObservableCollection<TransactionModel> transactions = new();

    double totalBalance = 0;
    string selectedType = "Income";
    string selectedIcon = "💰";
    string editSelectedIcon = "💰";
    TransactionModel? editingTransaction = null;
    string userId => App.UserId;

    public HomePage()
    {
        InitializeComponent();
        TransactionCollection.ItemsSource = transactions;
        _ = LoadTransactions();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        string name = Preferences.Get("user_display_name", "U");
        WelcomeLabel.Text = !string.IsNullOrEmpty(name) ? name : "Welcome back";
        AvatarLabel.Text = name.Length > 0 ? name[0].ToString().ToUpper() : "U";
    }

    // ── ICON SELECTION ────────────────────────────────
    private void OnIconSelected(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string icon)
        {
            selectedIcon = icon;
            SelectedIconLabel.Text = icon;
        }
    }

    private void OnEditIconSelected(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string icon)
        {
            editSelectedIcon = icon;
            EditSelectedIconLabel.Text = icon;
        }
    }

    // ── ADD FUNDS POPUP ───────────────────────────────
    private void OnAddFundsClicked(object sender, EventArgs e)
    {
        selectedType = "Income";
        selectedIcon = "💰";
        PopupTitle.Text = "Add Funds";
        FundsAmountEntry.Text = "";
        FundsDescriptionEntry.Text = "";
        SelectedIconLabel.Text = "💰";
        FundsOverlay.IsVisible = true;
    }

    private void OnMinusFundsClicked(object sender, EventArgs e)
    {
        selectedType = "Expense";
        selectedIcon = "💸";
        PopupTitle.Text = "Minus Funds";
        FundsAmountEntry.Text = "";
        FundsDescriptionEntry.Text = "";
        SelectedIconLabel.Text = "💸";
        FundsOverlay.IsVisible = true;
    }

    private async void OnUpdateFundsClicked(object sender, EventArgs e)
    {
        if (!double.TryParse(FundsAmountEntry.Text, out double amount) || amount <= 0)
        {
            await DisplayAlert("Error", "Enter a valid amount.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(userId))
        {
            await DisplayAlert("Error", "User ID is missing. Please log in again.", "OK");
            return;
        }

        string description = string.IsNullOrWhiteSpace(FundsDescriptionEntry.Text)
            ? (selectedType == "Income" ? "Funds were added" : "Funds were deducted")
            : FundsDescriptionEntry.Text.Trim();

        var firestoreData = new
        {
            fields = new
            {
                Title = new { stringValue = description },
                Icon = new { stringValue = selectedIcon },
                Amount = new { doubleValue = amount },
                Type = new { stringValue = selectedType },
                UserId = new { stringValue = userId }
            }
        };

        string json = JsonConvert.SerializeObject(firestoreData);
        using var client = new HttpClient();
        var idToken = Preferences.Get("idToken", null);

        if (string.IsNullOrEmpty(idToken))
        {
            await DisplayAlert("Error", "No login token found.", "OK");
            return;
        }

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        string url = "https://firestore.googleapis.com/v1/projects/project-f-c6e4e/databases/(default)/documents/transactions";

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Firestore Error", error, "OK");
            return;
        }

        FundsOverlay.IsVisible = false;
        await LoadTransactions();
    }

    private void OnCancelFundsClicked(object sender, EventArgs e)
        => FundsOverlay.IsVisible = false;

    // ── TRANSACTION TAP (EDIT/DELETE) ─────────────────
    private async void OnTransactionLongPressed(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not TransactionModel transaction) return;

        editingTransaction = transaction;
        editSelectedIcon = transaction.Icon ?? "💰";

        EditAmountEntry.Text = transaction.Amount.ToString("F2");
        EditDescriptionEntry.Text = transaction.Title;
        EditSelectedIconLabel.Text = editSelectedIcon;

        EditOverlay.IsVisible = true;
    }

    private async void OnSaveEditClicked(object sender, EventArgs e)
    {
        if (editingTransaction == null) return;

        if (!double.TryParse(EditAmountEntry.Text, out double amount) || amount <= 0)
        {
            await DisplayAlert("Error", "Enter a valid amount.", "OK");
            return;
        }

        string description = string.IsNullOrWhiteSpace(EditDescriptionEntry.Text)
            ? editingTransaction.Title ?? ""
            : EditDescriptionEntry.Text.Trim();

        var firestoreData = new
        {
            fields = new
            {
                Title = new { stringValue = description },
                Icon = new { stringValue = editSelectedIcon },
                Amount = new { doubleValue = amount },
                Type = new { stringValue = editingTransaction.Type ?? "Expense" },
                UserId = new { stringValue = userId }
            }
        };

        string json = JsonConvert.SerializeObject(firestoreData);
        using var client = new HttpClient();
        var idToken = Preferences.Get("idToken", null);

        if (string.IsNullOrEmpty(idToken)) return;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // PATCH to the document's full path
        string url = $"https://firestore.googleapis.com/v1/{editingTransaction.DocumentPath}?updateMask.fieldPaths=Title&updateMask.fieldPaths=Icon&updateMask.fieldPaths=Amount&updateMask.fieldPaths=Type&updateMask.fieldPaths=UserId";

        var response = await client.PatchAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Error", error, "OK");
            return;
        }

        EditOverlay.IsVisible = false;
        editingTransaction = null;
        await LoadTransactions();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (editingTransaction == null) return;

        bool confirm = await DisplayAlert("Delete", "Delete this transaction?", "Delete", "Cancel");
        if (!confirm) return;

        using var client = new HttpClient();
        var idToken = Preferences.Get("idToken", null);
        if (string.IsNullOrEmpty(idToken)) return;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        string url = $"https://firestore.googleapis.com/v1/{editingTransaction.DocumentPath}";
        var response = await client.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Error", error, "OK");
            return;
        }

        EditOverlay.IsVisible = false;
        editingTransaction = null;
        await LoadTransactions();
    }

    private void OnCancelEditClicked(object sender, EventArgs e)
    {
        EditOverlay.IsVisible = false;
        editingTransaction = null;
    }

    // ── LOAD TRANSACTIONS ─────────────────────────────
    private async Task LoadTransactions()
    {
        transactions.Clear();
        totalBalance = 0;

        using var client = new HttpClient();
        var idToken = Preferences.Get("idToken", null);

        if (string.IsNullOrEmpty(idToken))
        {
            await DisplayAlert("Error", "Not logged in.", "OK");
            return;
        }

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        string url = "https://firestore.googleapis.com/v1/projects/project-f-c6e4e/databases/(default)/documents/transactions";

        try
        {
            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var text = await response.Content.ReadAsStringAsync();
                await DisplayAlert("403 Forbidden", text, "OK");
                return;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            if (data?.documents == null) return;

            foreach (var doc in data.documents)
            {
                string uid = doc.fields.UserId.stringValue;
                if (uid != userId) continue;

                double amount = 0;
                if (doc.fields.Amount.doubleValue != null)
                    amount = (double)doc.fields.Amount.doubleValue;
                else if (doc.fields.Amount.integerValue != null)
                    amount = (double)doc.fields.Amount.integerValue;

                string icon = "💰";
                try { icon = doc.fields.Icon?.stringValue ?? "💰"; } catch { }

                string docName = doc.name;

                var transaction = new TransactionModel
                {
                    Title = doc.fields.Title.stringValue,
                    Icon = icon,
                    Amount = amount,
                    Type = doc.fields.Type.stringValue,
                    UserId = uid,
                    DocumentPath = docName
                };

                transactions.Add(transaction);

                if (transaction.Type == "Income")
                    totalBalance += transaction.Amount;
                else
                    totalBalance -= transaction.Amount;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BalanceLabel.Text = $"₱{totalBalance:F2}";
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ── NAV BAR ───────────────────────────────────────
    private async void OnHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    private async void OnCalendarClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//CalendarPage");

    private async void OnProfileClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//ProfilePage");
}