using Newtonsoft.Json;
using Project_F.Models;
using System.Globalization;

namespace Project_F.Pages;

public partial class CalendarPage : ContentPage
{
    DateTime _displayMonth;
    DateTime? _selectedDate;
    List<TransactionModel> _allTransactions = new();
    string userId => App.UserId;

    public CalendarPage()
    {
        InitializeComponent();
        _displayMonth = DateTime.Today;
        _selectedDate = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAllTransactions();
        BuildCalendar();
        ShowTransactionsForDate(_selectedDate ?? DateTime.Today);
    }

    // ── FIRESTORE FETCH ───────────────────────────────
    private async Task LoadAllTransactions()
    {
        _allTransactions.Clear();

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

                // Parse date from Firestore document name (last segment is doc ID, not date)
                // We store createTime from document metadata
                string createTime = doc.createTime ?? "";
                DateTime date = DateTime.Today;
                if (!string.IsNullOrEmpty(createTime))
                    DateTime.TryParse(createTime, out date);

                var transaction = new TransactionModel
                {
                    Title = doc.fields.Title.stringValue,
                    Icon = icon,
                    Amount = amount,
                    Type = doc.fields.Type.stringValue,
                    UserId = uid,
                    Date = date.ToLocalTime()
                };

                _allTransactions.Add(transaction);
            }
        }
        catch { }
    }

    // ── SHOW TRANSACTIONS FOR DATE ────────────────────
    private void ShowTransactionsForDate(DateTime date)
    {
        var filtered = _allTransactions
            .Where(t => t.Date.Date == date.Date)
            .ToList();

        DayTransactionCollection.ItemsSource = filtered;

        double total = 0;
        foreach (var t in filtered)
            total += t.Type == "Income" ? t.Amount : -t.Amount;

        DateLabel.Text = date.ToString("MMMM dd, yyyy");
        TotalAmountLabel.Text = $"₱{total:F2}";
        TotalAmountLabel.TextColor = total >= 0
            ? Color.FromArgb("#4CAF50")
            : Color.FromArgb("#FF5252");

        SummaryLabel.Text = filtered.Count == 0
            ? "No transactions"
            : $"{filtered.Count} transaction{(filtered.Count > 1 ? "s" : "")}";
    }

    // ── CALENDAR BUILDER ──────────────────────────────
    void BuildCalendar()
    {
        CalendarGrid.Children.Clear();
        CalendarGrid.RowDefinitions.Clear();
        for (int i = 0; i < 6; i++)
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        MonthLabel.Text = _displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        var firstOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
        int startCol = (int)firstOfMonth.DayOfWeek;
        int days = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);

        // Track which dates have transactions
        var datesWithTransactions = _allTransactions
            .Select(t => t.Date.Date)
            .ToHashSet();

        for (int day = 1; day <= days; day++)
        {
            int index = startCol + (day - 1);
            int row = index / 7;
            int col = index % 7;

            bool isSelected = _selectedDate.HasValue &&
                              _selectedDate.Value.Year == _displayMonth.Year &&
                              _selectedDate.Value.Month == _displayMonth.Month &&
                              _selectedDate.Value.Day == day;

            bool isToday = DateTime.Today.Year == _displayMonth.Year &&
                           DateTime.Today.Month == _displayMonth.Month &&
                           DateTime.Today.Day == day;

            var thisDate = new DateTime(_displayMonth.Year, _displayMonth.Month, day);
            bool hasTransactions = datesWithTransactions.Contains(thisDate.Date);

            var dayLabel = new Label
            {
                Text = day.ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14,
                FontAttributes = isToday ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isSelected
                    ? Colors.White
                    : isToday
                        ? Color.FromArgb("#FF8C00")
                        : Colors.White
            };

            // Dot indicator for days with transactions
            var dot = new BoxView
            {
                WidthRequest = 4,
                HeightRequest = 4,
                CornerRadius = 2,
                Color = hasTransactions ? Color.FromArgb("#FF8C00") : Colors.Transparent,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 0,
                Padding = new Thickness(2),
                Children = { dayLabel, dot }
            };

            var border = new Border
            {
                BackgroundColor = isSelected
                    ? Color.FromArgb("#FF8C00")
                    : isToday
                        ? Color.FromArgb("#2A2A2A")
                        : Colors.Transparent,
                StrokeThickness = 0,
                WidthRequest = 36,
                HeightRequest = 36,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = stack
            };
            border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };

            int capturedDay = day;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                _selectedDate = new DateTime(_displayMonth.Year, _displayMonth.Month, capturedDay);
                BuildCalendar();
                ShowTransactionsForDate(_selectedDate.Value);
            };
            border.GestureRecognizers.Add(tap);

            CalendarGrid.Add(border, col, row);
        }
    }

    // ── MONTH NAV ─────────────────────────────────────
    void OnPrevMonthClicked(object sender, EventArgs e)
    {
        _displayMonth = _displayMonth.AddMonths(-1);
        BuildCalendar();
        if (_selectedDate.HasValue)
            ShowTransactionsForDate(_selectedDate.Value);
    }

    void OnNextMonthClicked(object sender, EventArgs e)
    {
        _displayMonth = _displayMonth.AddMonths(1);
        BuildCalendar();
        if (_selectedDate.HasValue)
            ShowTransactionsForDate(_selectedDate.Value);
    }

    // ── NAV BAR ───────────────────────────────────────
    private async void OnHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    private async void OnCalendarClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//CalendarPage");

    private async void OnProfileClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//ProfilePage");
}