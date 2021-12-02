using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.Mixins;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shared;
using Shared.ML.Objects;
using Splat;

namespace ForecasterGUI.ViewModels
{
    public class ClosingPricesViewModel : ViewModelBase
    {
        private ObservableCollection<DateTimePoint> _observableCloseValues;
        private ObservableCollection<DateTimePoint> _observableSma;
        private ObservableCollection<DateTimePoint> _observableVolumeValues;

        public IEnumerable<ISeries> Series { get; set; }
        public IEnumerable<ICartesianAxis> XAxes { get; set; }
        public IEnumerable<ICartesianAxis> PriceAxes { get; set; }
        public IEnumerable<ISeries> VolumeSeries { get; set; }
        
        [Reactive]
        public DateTime StartDate { get; set; }
        
        public DateTime LastDate { get; private set; }
        
        private AppStateViewModel _appStateViewModel;
        
        public ReactiveCommand<Unit, Unit> RefitCommand { get; }

        public void RefitZoom()
        {
            XAxes.First().MinLimit = null;
            XAxes.First().MaxLimit = null;
            Trace.WriteLine($"{XAxes.First().MinLimit}");
            Trace.WriteLine($"{XAxes.First().MaxLimit}");
        }
 
        public ClosingPricesViewModel()
        {
            _appStateViewModel = Locator.Current.GetService<AppStateViewModel>()!;

            RefitCommand = ReactiveCommand.Create(RefitZoom);

            var closingPrices = _appStateViewModel.HlmcbavInfo
                .Where(p => p.Date >= DateTime.Now.AddYears(-2))
                .Select(p => new DateTimePoint(p.Date, p.Close)).ToList();
            var volumes = _appStateViewModel.HlmcbavInfo
                .Where(p => p.Date >= DateTime.Now.AddYears(-2))
                .Select(p => new DateTimePoint(p.Date, p.Volume)).ToList();

            var sma = Util.GetSimpleMovingAverage(
                    _appStateViewModel.HlmcbavInfo
                        .Where(p => p.Date >= DateTime.Now.AddYears(-2))
                        .Select(p => new TimedFeature(p.Date, Convert.ToSingle(p.Close))),
                    14)
                .Select(p => new DateTimePoint(p.Date, p.Feature));

            _observableCloseValues = new ObservableCollection<DateTimePoint>(closingPrices);
            _observableSma = new ObservableCollection<DateTimePoint>(sma);
            _observableVolumeValues = new ObservableCollection<DateTimePoint>(volumes);
            
            StartDate = _observableCloseValues.First().DateTime;
            LastDate = _observableCloseValues.Last().DateTime;

            this.WhenAnyValue(x => x.StartDate)
                .Subscribe(startDate =>
                {
                    if (startDate > LastDate) return;
                    
                    var filteredCloseData = closingPrices
                        .Where(d => d.DateTime >= startDate).ToList();
                    Trace.WriteLine($"Filtered date start date: {filteredCloseData.First().DateTime}");
                    _observableCloseValues.Clear();
                    _observableCloseValues.AddRange(filteredCloseData);

                    var filteredVolumeData = volumes
                        .Where(d => d.DateTime >= startDate).ToList();
                    _observableVolumeValues.Clear();
                    _observableVolumeValues.AddRange(filteredVolumeData);
                    
                    var filteredSmaData = sma 
                        .Where(d => d.DateTime >= startDate).ToList();
                    _observableSma.Clear();
                    _observableSma.AddRange(filteredSmaData);
                });
            
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<DateTimePoint>
                {
                    Name = "Close",
                    Values = _observableCloseValues,
                    Fill = null,
                    GeometrySize = 8,
                    TooltipLabelFormatter = point => $"Last: {point.Model!.Value:F2} on {point.Model!.DateTime.ToString("d")}"
                },
                new LineSeries<DateTimePoint>
                {
                    Name = "14 Day SMA",
                    Values = _observableSma,
                    Fill = null,
                    GeometrySize = 4,
                    TooltipLabelFormatter = point => $"SMA: {point.Model!.Value:F2}"
                }
            };
                
            VolumeSeries = new ObservableCollection<ISeries>
            {
                new ColumnSeries<DateTimePoint>
                {
                    Name = "Volume",
                    Values = _observableVolumeValues,
                    TooltipLabelFormatter = point => $"Vol: {point.Model!.Value:F2} on {point.Model!.DateTime.ToString("d")}"
                }
            };
            
            XAxes = new List<Axis>
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("MM/dd/yy"),
                    UnitWidth = TimeSpan.FromDays(1).Ticks
                }
            };

            PriceAxes = new List<Axis>
            {
                new Axis
                {
                    Labeler = Labelers.Currency
                }
            };
            
        }
    }
}