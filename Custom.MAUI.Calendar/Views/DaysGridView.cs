﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.MAUI.Calendar.Views
{
    internal class DaysGridView : Grid, IDisposable
    {
        public DateTime MinDate { get; set; } = new DateTime(1900, 1, 1);
        public DateTime MaxDate { get; set; } = new DateTime(2100, 12, 31);

        private int _currentYearPage = 0;

        public event EventHandler<int> MonthSelected;
        public event EventHandler<int> YearSelected;
        public event EventHandler<DateTime> DaySelected;

        private DateTime _currentDate = DateTime.Today;
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (_currentDate != value)
                {
                    _currentDate = value;
                    BuildGrid();
                }
            }
        }

        private List<DateTime> _selectedDates = new List<DateTime>();
        public List<DateTime> SelectedDates
        {
            get => _selectedDates;
            set
            {
                if (_selectedDates != value)
                {
                    _selectedDates = value;
                    BuildGrid();
                }
            }
        }

        private CultureInfo _culture = CultureInfo.CurrentCulture;
        public CultureInfo Culture
        {
            get => _culture;
            set
            {
                if (_culture != value)
                {
                    _culture = value;
                    BuildGrid();
                }
            }
        }

        private CalendarViewMode _viewMode = CalendarViewMode.Days;
        public CalendarViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode != value)
                {
                    _viewMode = value;
                    BuildGrid();
                }
            }
        }

        public CalendarStyle Style { get; set; }

        public DaysGridView()
        {
            _currentYearPage = (_currentDate.Year - MinDate.Year) / 12; 
            BuildGrid();
        }

        public void BuildGrid()
        {
            switch (_viewMode)
            {
                case CalendarViewMode.Days:
                    BuildDaysView();
                    break;
                case CalendarViewMode.Months:
                    BuildMonthsView();
                    break;
                case CalendarViewMode.Years:
                    BuildYearsView();
                    break;
            }
        }

        private void BuildBaseGrid(int rows, int columns)
        {
            Children.Clear();
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            for (int i = 0; i < rows; i++)
                RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            for (int i = 0; i < columns; i++)
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        private void BuildDaysView()
        {
            BuildBaseGrid(7, 7);
            AddDaysOfWeekLabels();

            RowSpacing = 5;
            ColumnSpacing = 5;
            VerticalOptions = LayoutOptions.Fill;
            HorizontalOptions = LayoutOptions.Fill;

            var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int startDay = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

            int row = 1;
            int column = startDay;

            for (int i = 0; i < startDay; i++)
            {
                var emptyLabel = new Label();
                Grid.SetRow(emptyLabel, row);
                Grid.SetColumn(emptyLabel, i);
                Children.Add(emptyLabel);
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDay = new DateTime(_currentDate.Year, _currentDate.Month, day);

                var dayButton = new Button
                {
                    Text = day.ToString(),
                    BindingContext = currentDay,
                    BackgroundColor = Style?.BackgroundColor ?? Colors.Transparent,
                    TextColor = Style?.DayTextColor ?? Colors.Black,
                    FontSize = Style?.DayFontSize ?? 14,
                    Padding = Style?.DayButtonPadding ?? new Thickness(5),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

                dayButton.Clicked += (s, e) => DaySelected?.Invoke(this, currentDay);

                // Выделение сегодняшнего дня
                if (currentDay.Date == DateTime.Today)
                {
                    dayButton.BackgroundColor = Style?.TodayBackgroundColor ?? Colors.LightBlue;
                }

                // Выделение выбранных дат
                if (_selectedDates != null && _selectedDates.Count > 0)
                {
                    if (_selectedDates.Count == 1)
                    {
                        if (currentDay.Date == _selectedDates[0].Date)
                        {
                            dayButton.BackgroundColor = Style?.SelectedDateBackgroundColor ?? Colors.LightGreen;
                        }
                    }
                    else if (_selectedDates.Count == 2)
                    {
                        if (currentDay.Date >= _selectedDates[0].Date && currentDay.Date <= _selectedDates[1].Date)
                        {
                            dayButton.BackgroundColor = Style?.DateRangeBackgroundColor ?? Colors.LightGreen;
                        }
                    }
                }

                Grid.SetRow(dayButton, row);
                Grid.SetColumn(dayButton, column);
                Children.Add(dayButton);

                column++;
                if (column > 6)
                {
                    column = 0;
                    row++;
                }
            }
        }

        private void BuildMonthsView()
        {
            int columns = 3;
            int rows = 4;
            BuildBaseGrid(rows, columns);

            var months = _culture.DateTimeFormat.MonthNames.Take(12).ToArray();

            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (index >= months.Length)
                        break;

                    var monthButton = new Button
                    {
                        Text = months[index],
                        CommandParameter = index + 1,
                        BackgroundColor = Style?.BackgroundColor ?? Colors.Transparent,
                        TextColor = Style?.LabelTextColor ?? Colors.Black,
                        FontSize = Style?.DayFontSize ?? 14
                    };
                    monthButton.Clicked += (s, e) =>
                    {
                        if (monthButton.CommandParameter is int month)
                        {
                            MonthSelected?.Invoke(this, month);
                        }
                    };

                    Grid.SetRow(monthButton, row);
                    Grid.SetColumn(monthButton, col);
                    Children.Add(monthButton);

                    index++;
                }
            }
        }

        private void BuildYearsView()
        {
            int columns = 3;
            int rows = 5;
            BuildBaseGrid(rows, columns);

            int startYear = _currentYearPage * 12 + MinDate.Year;
            int endYear = startYear + 11;
            if (endYear > MaxDate.Year)
            {
                endYear = MaxDate.Year;
            }

            int year = startYear;
            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (year > MaxDate.Year)
                        break;

                    var yearButton = new Button
                    {
                        Text = year.ToString(),
                        CommandParameter = year,
                        BackgroundColor = Style?.BackgroundColor ?? Colors.Transparent,
                        TextColor = Style?.LabelTextColor ?? Colors.Black,
                        FontSize = Style?.DayFontSize ?? 14
                    };
                    yearButton.Clicked += (s, e) =>
                    {
                        if (yearButton.CommandParameter is int selectedYear)
                        {
                            YearSelected?.Invoke(this, selectedYear);
                        }
                    };

                    Grid.SetRow(yearButton, row);
                    Grid.SetColumn(yearButton, col);
                    Children.Add(yearButton);

                    year++;
                }
            }

            // Добавляем кнопки навигации для годов
            var previousYearPageButton = CreateNavigationButton("<", OnPreviousYearPageClicked);
            var nextYearPageButton = CreateNavigationButton(">", OnNextYearPageClicked);

            Grid.SetRow(previousYearPageButton, rows - 1);
            Grid.SetColumn(previousYearPageButton, 0);
            Children.Add(previousYearPageButton);

            Grid.SetRow(nextYearPageButton, rows - 1);
            Grid.SetColumn(nextYearPageButton, columns - 1);
            Children.Add(nextYearPageButton);
        }

        private void OnPreviousYearPageClicked(object sender, EventArgs e)
        {
            if (_currentYearPage > 0)
            {
                _currentYearPage--;
                BuildGrid();
            }
        }

        private void OnNextYearPageClicked(object sender, EventArgs e)
        {
            if ((_currentYearPage + 1) * 12 + MinDate.Year <= MaxDate.Year)
            {
                _currentYearPage++;
                BuildGrid();
            }
        }

        private Button CreateNavigationButton(string text, EventHandler clickedHandler)
        {
            var button = new Button
            {
                Text = text,
                WidthRequest = Style?.NavigationButtonSize ?? 40,
                HeightRequest = Style?.NavigationButtonSize ?? 40,
                Padding = Style?.NavigationButtonPadding ?? new Thickness(5),
                BackgroundColor = Style?.NavigationButtonBackgroundColor ?? Colors.LightGray,
                TextColor = Style?.NavigationButtonTextColor ?? Colors.Black,
                CornerRadius = (int)(Style?.NavigationButtonCornerRadius ?? 20)
            };
            button.Clicked += clickedHandler;
            return button;
        }

        private void AddDaysOfWeekLabels()
        {
            var firstDayOfWeek = _culture.DateTimeFormat.FirstDayOfWeek;
            var dayNames = _culture.DateTimeFormat.AbbreviatedDayNames;

            for (int i = 0; i < 7; i++)
            {
                int dayIndex = (i + (int)firstDayOfWeek) % 7;
                var dayLabel = new Label
                {
                    Text = dayNames[dayIndex],
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Style?.DayOfWeekLabelTextColor ?? Colors.Black,
                    FontSize = Style?.DayOfWeekLabelFontSize ?? 14
                };
                Grid.SetColumn(dayLabel, i);
                Grid.SetRow(dayLabel, 0);
                Children.Add(dayLabel);
            }
        }

        public void Dispose()
        {
            Children.Clear();
        }
    }

}
