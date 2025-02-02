﻿using System.Reactive.Disposables;
using Shiny.Health;

namespace Sample;


public class HealthTestViewModel : ViewModel
{
    readonly IDeviceDisplay display;


    public HealthTestViewModel(
        BaseServices services,
        IDeviceDisplay display,
        IHealthService health
    ) : base(services)
    {
        this.display = display;

        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await health.RequestPermission(
                new Permission(DistanceHealthMetric.Default, PermissionType.Read),
                new Permission(CaloriesHealthMetric.Default, PermissionType.Read),
                new Permission(StepCountHealthMetric.Default, PermissionType.Read),
                new Permission(HeartRateHealthMetric.Default, PermissionType.Read)
            );
            if (!result)
            {
                await this.Dialogs.Alert("Failed permission check");
                return;
            }

            var start = (DateTimeOffset)this.Start.ToDateTime(TimeOnly.MinValue);
            var end = (DateTimeOffset)this.End.ToDateTime(TimeOnly.MaxValue);
            if (start < end)
            {
                this.ErrorText = String.Empty;
            }
            else
            {
                this.ErrorText = "Start date must be greater than End date";
                this.Calories = 0;
                this.HeartRate = 0;
                this.Distance = 0;
                this.Steps = 0;
                return;
            }

            this.Distance = (await health.Query(DistanceHealthMetric.Default, start, end, Interval.Days)).Sum(x => x.Value);
            this.Calories = (await health.Query(CaloriesHealthMetric.Default, start, end, Interval.Days)).Sum(x => x.Value);            
            this.Steps = (await health.Query(StepCountHealthMetric.Default, start, end, Interval.Days)).Sum(x => x.Value);
            this.HeartRate = (await health.Query(HeartRateHealthMetric.Default, start, end, Interval.Days)).Average(x => x.Value);
        });
        this.BindBusyCommand(this.Load);
    }


    public ICommand Load { get; }
    [Reactive] public DateOnly Start { get; set; }
    [Reactive] public DateOnly End { get; set; }

    [Reactive] public string ErrorText { get; private set; }
    [Reactive] public int Steps { get; private set; }
    [Reactive] public double Calories { get; private set; }
    [Reactive] public double Distance { get; private set; }
    [Reactive] public double HeartRate { get; private set; }

    //[Reactive] public List<MonitorViewModel> Monitors { get; private set; }

    public override void OnAppearing()
    {
        base.OnAppearing();
        display.KeepScreenOn = true;
        this.DeactivateWith.Add(Disposable.Create(() => display.KeepScreenOn = false));

        this.Start = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        this.End = DateOnly.FromDateTime(DateTime.Now);

        this.WhenAnyValue(x => x.Start)
            .Skip(1)
            .Subscribe(_ => this.Load.Execute(null))
            .DisposedBy(this.DeactivateWith);

        this.WhenAnyValue(x => x.End)
            .Skip(1)
            .Subscribe(_ => this.Load.Execute(null))
            .DisposedBy(this.DeactivateWith);

        //this.Load.Execute(null);
        //this.Monitors = new List<MonitorViewModel>
        //{
        //    new MonitorViewModel("Steps (total)", health.MonitorSteps().RunningAccumulation().Select(x => x.ToString())),
        //    new MonitorViewModel("Calories (total)", health.MonitorCalories().RunningAccumulation().Select(x => x.ToString())),
        //    new MonitorViewModel("Distance (total)", health.MonitorDistance().RunningAccumulation().Select(x => x.ToString())),
        //    new MonitorViewModel("Heart Rate (bpm)", health.MonitorHeartRate().Select(x => x.ToString()))
        //};
        //this.Monitors.ForEach(disp.Add);
    }
}

/*
public class MonitorViewModel : ReactiveObject, IDisposable
{
    readonly string description;

    public MonitorViewModel(string description, IObservable<string> observable)
    {
        this.description = description;
        this.StatusText = "Monitor " + this.description;
        this.Value = String.Empty;

        this.Toggle = ReactiveCommand.Create(() =>
        {
            if (this.disp == null)
            {
                this.disp = observable.SubOnMainThread(x =>
                {
                    Console.WriteLine($"MONITOR VALUE: {this.description} - {x}");
                    this.Value = x;
                });
                this.StatusText = "Monitoring " + this.description;
            }
            else
            {
                this.Stop();
            }
        });
    }


    [Reactive] public string StatusText { get; private set; }
    [Reactive] public string Value { get; private set; }
    public ICommand Toggle { get; }

    IDisposable? disp;
    void Stop()
    {
        this.disp?.Dispose();
        this.disp = null;
        this.StatusText = "Monitor " + this.description;
        this.Value = String.Empty;
    }

    public void Dispose() => this.Stop();
}
*/