using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using PomodoroTimer.Helpers;
using PomodoroTimer.Models;


namespace PomodoroTimer
{
    public partial class MainPage : ContentPage
    {
        // Sabit süreler (saniye cinsinden)
        private const int PomodoroDuration = 25 * 60;
        private const int ShortBreakDuration = 5 * 60;
        private const int LongBreakDuration = 15 * 60;
        private int pomodoroCounter = 0;

        // Zaman bilgileri
        private int totalTimeInSeconds;
        private int minute;
        private int second;

        // Durum yönetimi
        private string timeStatus = "Pomodoro";
        private int pomodoroCount = 0;
        private bool isTimerRunning = false;

        // Sayaç ve iptal kontrolü
        private CancellationTokenSource? cancellationTokenSource;

        // Görev ekleme için
        private int gridRow = 0;


        private List<TaskItem> taskList = new();
        private int completedPomodoros = 0;


        public MainPage()
        {
            InitializeComponent();
            OnClickedPomodoro(pomodoroButton,EventArgs.Empty);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            taskList = await DataStorage.LoadTasksAsync();
            completedPomodoros = await DataStorage.LoadPomodoroCountAsync();

            LoadTasksToGrid();
        }

        private void LoadTasksToGrid()
        {
            tasksGrid.Children.Clear();
            tasksGrid.RowDefinitions.Clear();
            gridRow = 0;

            foreach (var task in taskList)
            {
                tasksGrid.AddRowDefinition(new RowDefinition { Height = GridLength.Auto });

                var checkBox = new CheckBox
                {
                    IsChecked = task.IsCompleted,
                    HorizontalOptions = LayoutOptions.Start
                };

                checkBox.CheckedChanged += async (s, e) =>
                {
                    task.IsCompleted = checkBox.IsChecked;
                    await DataStorage.SaveTasksAsync(taskList);
                };

                var label = new Label
                {
                    Text = task.Text,
                    MaximumWidthRequest = 200,
                    LineBreakMode = LineBreakMode.WordWrap,
                    VerticalOptions = LayoutOptions.Center
                    

                };

                var taskLayout = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
                };

                taskLayout.Children.Add(checkBox);
                taskLayout.SetRow(checkBox, 0);
                taskLayout.SetColumn(checkBox, 0);
                taskLayout.Children.Add(label);
                taskLayout.SetRow(label, 0);
                taskLayout.SetColumn(label, 1);

                var swipeView = new SwipeView
                {
                    RightItems = new SwipeItems
            {
                new SwipeItem
                {
                    Text = "Delete",
                    BackgroundColor = Colors.Red,
                    Command = new Command(async () =>
                    {
                        taskList.Remove(task);
                        await DataStorage.SaveTasksAsync(taskList);
                        LoadTasksToGrid();
                    })
                }
            },
                    Content = taskLayout
                };

                tasksGrid.Children.Add(swipeView);
                tasksGrid.SetRow(swipeView, gridRow);
                tasksGrid.SetColumnSpan(swipeView, 2);

                gridRow++;
            }
        }

        


        private void OnClickedRefresh(object sender, EventArgs e)
        {
            pomodoroCounter = 0;
            pomodoroLabel.Text = $"{pomodoroCounter} Pomodoro Completed";
        }
        private async Task PlayNotificationSound()
        {
            var player = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("sound-effect.mp3"));
            player.Play();
        }


        // --- ZAMAN SEÇİM BUTONLARI ---

        private void OnClickedPomodoro(object sender, EventArgs e)
        {
            timeStatus = "Pomodoro";
            totalTimeInSeconds = PomodoroDuration;
            UpdateTimeLabel();
        }

        private void OnClickedShortBreak(object sender, EventArgs e)
        {
            timeStatus = "ShortBreak";
            totalTimeInSeconds = ShortBreakDuration;
            UpdateTimeLabel();
        }

        private void OnClickedLongBreak(object sender, EventArgs e)
        {
            timeStatus = "LongBreak";
            totalTimeInSeconds = LongBreakDuration;
            UpdateTimeLabel();
        }

        // --- BAŞLAT / DURDUR BUTONU ---

        private async void OnClickStartStop(object sender, EventArgs e)
        {
            if (!isTimerRunning)
            {
                cancellationTokenSource = new CancellationTokenSource();
                isTimerRunning = true;
                startStopButton.Text = "Stop";
                await StartCountDown(cancellationTokenSource.Token);
            }
            else
            {
                isTimerRunning = false;
                cancellationTokenSource?.Cancel();
                startStopButton.Text = "Start";
            }
        }


        // --- SAYAÇ BAŞLATMA METODU ---

        private async Task StartCountDown(CancellationToken token)
        {
            try
            {
                while (totalTimeInSeconds > 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        isTimerRunning = false;
                        break;
                    }

                    //COUNTER TEST HERE!!!!!
                    await Task.Delay(10, token); // 1 saniyelik gerçek bekleme
                    totalTimeInSeconds--;

                    minute = totalTimeInSeconds / 60;
                    second = totalTimeInSeconds % 60;

                    // UI güncellemesi
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        timeLabel.Text = $"{minute:D2}:{second:D2}";
                    });
                }

                // Süre bittiyse OnTimerCompleted çağrılır
                if (totalTimeInSeconds <= 0 && !token.IsCancellationRequested)
                {
                    await OnTimerCompleted(); // async olduğu için await et
                }

                isTimerRunning = false;
                startStopButton.Text = "Start";
            }
            catch (TaskCanceledException)
            {
                // iptal edildiyse hata fırlatma
            }
        }


        // --- TAMAMLANDIĞINDA GEÇİŞLER ---

        private async Task OnTimerCompleted()
        {
            await PlayNotificationSound();

            if (timeStatus == "Pomodoro")
            {
                completedPomodoros++;
                await DataStorage.SavePomodoroCountAsync(completedPomodoros);
            }



            switch (timeStatus)
            {
                case "Pomodoro":
                    pomodoroCount++;
                    pomodoroCounter++;
                    if (pomodoroCount % 4 == 0)
                    {
                        timeStatus = "LongBreak";
                        totalTimeInSeconds = LongBreakDuration;
                    }
                    else
                    {
                        timeStatus = "ShortBreak";
                        totalTimeInSeconds = ShortBreakDuration;
                    }
                    break;

                case "ShortBreak":
                case "LongBreak":
                    timeStatus = "Pomodoro";
                    totalTimeInSeconds = PomodoroDuration;
                    break;
            }

            pomodoroLabel.Text = $"{pomodoroCounter} Pomodoro Completed";
            UpdateTimeLabel(); // Yeni süreyi UI'a yaz

            await Task.Delay(500); // Yarım saniye bekle

            if (isTimerRunning)
            {
                cancellationTokenSource = new CancellationTokenSource();
                await StartCountDown(cancellationTokenSource.Token);
            }
        }




        // --- ZAMAN GÜNCELLEYEN FONKSİYON ---

        private void UpdateTimeLabel()
        {
            minute = totalTimeInSeconds / 60;
            second = totalTimeInSeconds % 60;
            timeLabel.Text = $"{minute:D2}:{second:D2}";
        }

        // --- GÖREV EKLEME BUTONU ---


        private async void AddTask(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(taskEntry.Text))
                return;

            var newTask = new TaskItem { Text = taskEntry.Text, IsCompleted = false };
            taskList.Add(newTask);
            await DataStorage.SaveTasksAsync(taskList);

            taskEntry.Text = string.Empty;
            LoadTasksToGrid();
        }



    }
}
