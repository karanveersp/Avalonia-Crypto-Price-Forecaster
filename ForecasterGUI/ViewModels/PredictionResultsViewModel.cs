using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForecasterGUI.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Shared;
using Shared.ML.Objects;
using SkiaSharp;

namespace ForecasterGUI.ViewModels
{
    public class PredictionResultsViewModel : ViewModelBase
    {
        public IEnumerable<ISeries> SeriesCollection { get; set; }
        public IEnumerable<ICartesianAxis> XAxes { get; set; }


        public PredictionResultsViewModel(PredictionData predictionData, string datasetFile)
        {
            var trainingData = HlmcbavData.FromFile(datasetFile)
                .Select(hlmcbav => new DateTimePoint(hlmcbav.Date, hlmcbav.Close));

            var forecastData = predictionData.Forecast
                .Select(f =>
                    new FinancialPoint(f.Date, f.UpperBound, f.Forecast - 0.1, f.Forecast, f.LowerBound)
                ).ToList();

            SeriesCollection = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = "Training data",
                    Values = trainingData.TakeLast(forecastData.Count * 3).ToList(),
                    GeometryFill = new SolidColorPaint(SKColors.SkyBlue),
                    GeometrySize = 15,
                    Fill = null,
                    TooltipLabelFormatter = point => 
                        $"Close: {point.Model.Value:F2} on {point.Model.DateTime.ToString("d")}"
                },
                new CandlesticksSeries<FinancialPoint>
                {
                    Name = "Forecast with error range",
                    Values = forecastData,
                    TooltipLabelFormatter = point => $"Predicted Close: {point.Model!.Close:F2} on {point.Model!.Date.ToString("d")}"
                },
            };
            
            XAxes = new List<Axis>
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("MM/dd/yy"),
                    UnitWidth = TimeSpan.FromDays(1).Ticks
                }
            };
        }
        
    }
}