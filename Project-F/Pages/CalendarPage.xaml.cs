using System;
using Microsoft.Maui.Controls;
using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Project_F.Pages
{
    public partial class CalendarPage : ContentPage
    {
        DateTime _displayMonth;
        DateTime? _selectedDate;

        public CalendarPage()
        {
            InitializeComponent();
            _displayMonth = DateTime.Today;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            BuildCalendar();
        }

        void OnPrevMonthClicked(object sender, EventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            BuildCalendar();
        }

        void OnNextMonthClicked(object sender, EventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(1);
            BuildCalendar();
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//HomePage");
        }

        private async void OnCalendarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CalendarPage");
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }

        void BuildCalendar()
        {
            CalendarGrid.Children.Clear();

            var monthName = _displayMonth.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant();
            MonthLabel.Text = monthName;
            MonthLabel.TextColor = GetColor("White", "#FFFFFF");

            string[] weekdays = new[] { "S", "M", "T", "W", "T", "F", "S" };
            for (int c = 0; c < 7; c++)
            {
                var lbl = new Label
                {
                    Text = weekdays[c],
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 15,
                    TextColor = (Color)Resources["SubText"]
                };
                if (c == 0 || c == 6)
                    lbl.TextColor = GetColor("Orange", "#FF6600");

                CalendarGrid.Add(lbl, c, 0);
            }

            var firstOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
            int startCol = (int)firstOfMonth.DayOfWeek;
            int days = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);

            for (int day = 1; day <= days; day++)
            {
                int index = startCol + (day - 1);
                int row = 1 + index / 7;
                int col = index % 7;

                bool isSelected = _selectedDate.HasValue &&
                                  _selectedDate.Value.Year == _displayMonth.Year &&
                                  _selectedDate.Value.Month == _displayMonth.Month &&
                                  _selectedDate.Value.Day == day;

                var dayLabel = new Label
                {
                    Text = day.ToString(),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 15,
                    TextColor = isSelected ? GetColor("BgColor", "#512BD4") : GetColor("White", "#FFFFFF")
                };

                var frame = new Frame
                {
                    Padding = 6,
                    CornerRadius = 8,
                    HasShadow = false,
                    BackgroundColor = isSelected ? GetColor("Orange", "#FF6600") : Colors.Transparent,
                    Content = dayLabel
                };

                var tap = new TapGestureRecognizer();
                int capturedDay = day;
                tap.Tapped += (s, ev) =>
                {
                    _selectedDate = new DateTime(_displayMonth.Year, _displayMonth.Month, capturedDay);
                    var monthProper = _selectedDate.Value.ToString("MMMM", CultureInfo.CurrentCulture);
                    DateLabel.Text = $"on {monthProper} {_selectedDate.Value.Day} {_selectedDate.Value.Year}";
                    BuildCalendar();
                };
                frame.GestureRecognizers.Add(tap);

                CalendarGrid.Add(frame, col, row);
            }

            if (!_selectedDate.HasValue || _selectedDate.Value.Year != _displayMonth.Year || _selectedDate.Value.Month != _displayMonth.Month)
            {
                DateLabel.Text = $"on {_displayMonth.ToString("MMMM", CultureInfo.CurrentCulture)} {_displayMonth.Year}";
            }
        }

        Color GetColor(string key, string fallbackHex)
        {
            if (Resources != null && Resources.ContainsKey(key) && Resources[key] is Color local)
                return local;

            if (Application.Current?.Resources != null && Application.Current.Resources.ContainsKey(key) &&
                Application.Current.Resources[key] is Color appColor)
                return appColor;

            return Color.FromArgb(fallbackHex);
        }
    }
}