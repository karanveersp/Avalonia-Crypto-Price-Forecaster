using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using Avalonia.Media;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shared;
using Shared.ML.Objects;
using SkiaSharp;
using Splat;

namespace ForecasterGUI.ViewModels
{
    public class PercentChangesViewModel : ViewModelBase
    {
        private static SolidColorPaint _underThresholdColor = new SolidColorPaint(SKColors.LightBlue);
        private static SolidColorPaint _overThresholdColor = new SolidColorPaint(SKColors.Green);

        private ObservableCollection<DateTimePoint> _observableCloseValues;

        [Reactive]
        public DateTime StartDate { get; set; }
        
        public DateTime LastDate { get; private set; }
        
        public IEnumerable<TimedFeature> ClosingPrices { get; }
        public IEnumerable<ISeries> ClosingPriceSeries { get; set; }
        public ObservableCollection<ISeries> PercentSeries { get; set; }
        public IEnumerable<ICartesianAxis> XAxes { get; set; }

        [Reactive] public int Period { get; set; }
        [Reactive] public double Threshold { get; set; }

        public ReactiveCommand<Unit, Unit> Recompute { get; private set; }
        public ReactiveCommand<Unit, Unit> RefitCommand { get; }

        public void RefitZoom()
        {
            XAxes.First().MinLimit = null;
            XAxes.First().MaxLimit = null;
            Trace.WriteLine($"{XAxes.First().MinLimit}");
            Trace.WriteLine($"{XAxes.First().MaxLimit}");
            Trace.WriteLine("Performed refitting!");
        }
        
        public void RecalculateValues()
        {
            if (StartDate >= LastDate)
                return;
            var dataAfterDate = _observableCloseValues
                .Select(p => new TimedFeature(p.DateTime, Convert.ToSingle(p.Value)))
                .Where(p => p.Date.Date >= StartDate);
            var nDayPercentChanges = Util.ToPercentChanges(dataAfterDate.ToArray(), Period);
            var data = nDayPercentChanges.Where(p => p != null).Select(
                p => new DateTimePoint(p.Date, p.Feature)).ToList();
            PercentSeries.Clear();
            PercentSeries.AddRange(ToColumnSeries(data));
            Trace.WriteLine("Recalculated percent series!");

        }

        private ColumnSeries<DateTimePoint>[] ToColumnSeries(List<DateTimePoint> data)
        {
            
            var underThresholdCols = new ColumnSeries<DateTimePoint>
            {
                Values = data.Where(p => Math.Abs(p.Value!.Value) < Threshold),
                Name = $"{Period} Day Percent Change Under {Threshold:F2}",
                TooltipLabelFormatter = pt =>
                    $"{pt.Model!.Value / 100:P} on {pt.Model.DateTime:d}",
                Fill = _underThresholdColor
            };
            var overThresholdCols = new ColumnSeries<DateTimePoint>
            {
                Values = data.Where(p => Math.Abs(p.Value!.Value) >= Threshold),
                Name = $"{Period} Day Percent Change >= {Threshold:F2}",
                TooltipLabelFormatter = pt =>
                    $"{pt.Model!.Value / 100:P} on {pt.Model.DateTime:d}",
                Fill = _overThresholdColor
            };
            return new[] { underThresholdCols, overThresholdCols };
        }

        private AppStateViewModel _appStateViewModel;
        
        public PercentChangesViewModel()
        {
            _appStateViewModel = Locator.Current.GetService<AppStateViewModel>()!;
            ClosingPrices = Util.ToClosingPrices(_appStateViewModel.HlmcbavInfo)
                .Where(p => p.Date >= DateTime.Now.AddYears(-1)) // only keep past 1 year of data
                .ToList();

            _observableCloseValues = new ObservableCollection<DateTimePoint>(ClosingPrices.Select(p => new DateTimePoint(p.Date, p.Feature)));
            
            Period = 3;
            Threshold = 5.0;

            Recompute = ReactiveCommand.Create(RecalculateValues);
            RefitCommand = ReactiveCommand.Create(RefitZoom);
            
            StartDate = _observableCloseValues.First().DateTime;
            LastDate = _observableCloseValues.Last().DateTime;

            var nDayPercentChanges = Util.ToPercentChanges(ClosingPrices.ToArray(), Period);
            var data = nDayPercentChanges.Select(
                p => new DateTimePoint(p.Date, p.Feature)).ToList();
            
            XAxes = new ObservableCollection<ICartesianAxis>
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("MM/dd/yy"),
                    UnitWidth = TimeSpan.FromDays(1).Ticks
                }
            };

            ClosingPriceSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<DateTimePoint>
                {
                    Values = _observableCloseValues,
                    Name = "Closing Price",
                    TooltipLabelFormatter = point => $"Last: {point.Model!.Value:F2} on {point.Model!.DateTime.ToString("d")}",
                    GeometrySize = 10,
                    Fill = null
                }
            };
            
            PercentSeries = new ObservableCollection<ISeries>();
            PercentSeries.AddRange(ToColumnSeries(data));

            this.WhenAnyValue(x => x.StartDate)
                .Subscribe(startDate =>
                {
                    if (startDate > LastDate) return;
                    
                    var filteredCloseData = ClosingPrices 
                        .Where(d => d.Date >= startDate)
                        .Select(p => new DateTimePoint(p.Date, p.Feature)).ToList();
                    Trace.WriteLine($"Filtered date start date: {filteredCloseData.First().DateTime}");
                    _observableCloseValues.Clear();
                    _observableCloseValues.AddRange(filteredCloseData);

                    RecalculateValues();
                    RefitZoom();
                });
        }
    }
}