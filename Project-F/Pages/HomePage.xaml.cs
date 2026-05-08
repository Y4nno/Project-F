using Newtonsoft.Json;
using Project_F.Models;
using System.Collections.ObjectModel;
using System.Text;

namespace Project_F.Pages;

public partial class HomePage : ContentPage
{
    ObservableCollection<TransactionModel> transactions = new();

    double totalBalance = 0;
    double totalIncome = 0;
    double totalExpenses = 0;

    string selectedType = "Income";

    string projectId = "project-f-c6e4e";
    string userId => App.UserId;

    public HomePage()
    {
        InitializeComponent();
        TransactionCollection.ItemsSource = transactions;
        _ = LoadTransactions();
    }

    private void OnIncomeTypeSelected(object sender, EventArgs e)
    {
        selectedType = "Income";
        IncomeBtn.BackgroundColor = Color.FromArgb("#FF8C001A");
        IncomeBtn.TextColor = Color.FromArgb("#FF8C00");
        IncomeBtn.BorderColor = Color.FromArgb("#FF8C00");
        ExpenseBtn.BackgroundColor = Color.FromArgb("#2A2A2A");
        ExpenseBtn.TextColor = Color.FromArgb("#888888");
        ExpenseBtn.BorderColor = Color.FromArgb("#333333");
    }

    private void OnExpenseTypeSelected(object sender, EventArgs e)
    {
        selectedType = "Expense";
        ExpenseBtn.BackgroundColor = Color.FromArgb("#FF8C001A");
        ExpenseBtn.TextColor = Color.FromArgb("#FF8C00");
        ExpenseBtn.BorderColor = Color.FromArgb("#FF8C00");
        IncomeBtn.BackgroundColor = Color.FromArgb("#2A2A2A");
        IncomeBtn.TextColor = Color.FromArgb("#888888");
        IncomeBtn.BorderColor = Color.FromArgb("#333333");
    }

    private async void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text) ||
            string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            await DisplayAlert("Error", "Fill all fields.", "OK");
            return;
        }

        if (!double.TryParse(AmountEntry.Text, out double amount))
        {
            await DisplayAlert("Error", "Invalid amount.", "OK");
            return;
        }

        var transaction = new TransactionModel
        {
            Title = TitleEntry.Text,
            Amount = amount,
            Type = selectedType,
            UserId = userId
        };

        var firestoreData = new
        {
            fields = new
            {
                Title = new { stringValue = transaction.Title },
                Amount = new { doubleValue = transaction.Amount },
                Type = new { stringValue = transaction.Type },
                UserId = new { stringValue = transaction.UserId }
            }
        };

        string json = JsonConvert.SerializeObject(firestoreData);

        using var client = new HttpClient();

        var idToken = Preferences.Get("idToken", null);

        if (string.IsNullOrEmpty(idToken))
        {
            await DisplayAlert("Error", "No login token found. Please login again.", "OK");
            return;
        }

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        string url =
            $"https://firestore.googleapis.com/v1/projects/project-f-c6e4e/databases/(default)/documents/transactions";

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Firestore Error", error, "OK");
            return;
        }

        TitleEntry.Text = "";
        AmountEntry.Text = "";
        OnIncomeTypeSelected(null, null);

        await LoadTransactions();
    }

    private async Task LoadTransactions()
    {
        transactions.Clear();
        totalBalance = 0;
        totalIncome = 0;
        totalExpenses = 0;

        using var client = new HttpClient();

        var idToken = Preferences.Get("idToken", null);

        if (string.IsNullOrEmpty(idToken))
        {
            await DisplayAlert("Error", "Not logged in.", "OK");
            return;
        }

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

        string url =
            $"https://firestore.googleapis.com/v1/projects/project-f-c6e4e/databases/(default)/documents/transactions";

        try
        {
            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var text = await response.Content.ReadAsStringAsync();
                await DisplayAlert(
                    "403 Forbidden",
                    "Firebase rejected request.\nCheck Firestore rules or login token.\n\n" + text,
                    "OK");
                return;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            if (data?.documents == null)
                return;

            foreach (var doc in data.documents)
            {
                string uid = doc.fields.UserId.stringValue;

                if (uid != userId)
                    continue;

                double amount = 0;

                if (doc.fields.Amount.doubleValue != null)
                    amount = (double)doc.fields.Amount.doubleValue;
                else if (doc.fields.Amount.integerValue != null)
                    amount = (double)doc.fields.Amount.integerValue;

                var transaction = new TransactionModel
                {
                    Title = doc.fields.Title.stringValue,
                    Amount = amount,
                    Type = doc.fields.Type.stringValue,
                    UserId = uid
                };

                transactions.Add(transaction);

                if (transaction.Type == "Income")
                {
                    totalIncome += transaction.Amount;
                    totalBalance += transaction.Amount;
                }
                else
                {
                    totalExpenses += transaction.Amount;
                    totalBalance -= transaction.Amount;
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BalanceLabel.Text = $"₱{totalBalance:F2}";
                IncomeLabel.Text = $"₱{totalIncome:F2}";
                ExpenseLabel.Text = $"₱{totalExpenses:F2}";
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}