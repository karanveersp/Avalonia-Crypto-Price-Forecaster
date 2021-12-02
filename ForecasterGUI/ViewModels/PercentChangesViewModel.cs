using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public IEnumerable<ISeries> ClosingPriceSeries { get; set; }
        public ObservableCollection<ISeries> Series { get; set; }
        public IEnumerable<ICartesianAxis> XAxes { get; set; }

        [Reactive] public int Period { get; set; }
        [Reactive] public double Threshold { get; set; }

        public ReactiveCommand<Unit, Unit> Recompute { get; private set; }

        public void ReacalculateValues()
        {
            var nDayPercentChanges = Util.ToPercentChanges(ClosingPrices.ToArray(), Period);
            var data = nDayPercentChanges.Where(p => p != null).Select(
                p => new DateTimePoint(p.Date, p.Feature)).ToList();
            Series.Clear();
            Series.AddRange(ToColumnSeries(data));
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
        
        
        public IEnumerable<TimedFeature> ClosingPrices { get; }

        public PercentChangesViewModel()
        {
            _appStateViewModel = Locator.Current.GetService<AppStateViewModel>()!;
            ClosingPrices = Util.ToClosingPrices(_appStateViewModel.HlmcbavInfo)
                .Where(d => d.Date >= DateTime.Now.AddMonths(-3)).ToList();

            Period = 3;
            Threshold = 5.0;

            Recompute = ReactiveCommand.Create(ReacalculateValues);

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
                    Values = ClosingPrices.Select(p => new DateTimePoint(p.Date, p.Feature)).ToList(),
                    Name = "Closing Price for last 3 months",
                    TooltipLabelFormatter = point => $"Last: {point.Model!.Value:F2} on {point.Model!.DateTime.ToString("d")}",
                    GeometrySize = 10,
                    Fill = null
                }
            };

            Series = new ObservableCollection<ISeries>();
            Series.AddRange(ToColumnSeries(data));
        }
    }
}