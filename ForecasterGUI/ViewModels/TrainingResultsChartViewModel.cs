using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using ForecasterGUI.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Shared;
using SkiaSharp;

namespace ForecasterGUI.ViewModels
{
    public class TrainingResultsChartViewModel : ViewModelBase
    {
        public int Horizon { get; }
        public int SeriesLength { get; }
        public int WindowSize { get; }
        
        public double MeanForecastError { get; }
        public double MeanAbsoluteError { get; }
        public double MeanSquaredError { get; }
        
        public DateTime TrainedToDate { get; set; }
        public DateTime TrainedFromDate { get; set; }
        
        public IEnumerable<ISeries> SeriesCollection { get; set; }
        public IEnumerable<ICartesianAxis> XAxes { get; set; }

        public TrainingResultsChartViewModel(MlModel model)
        {
            if (model.Metadata == null)
                throw new NullReferenceException($"{model.Name} metadata file not found. Please retrain the model.");
            
            Horizon = model.Metadata.Horizon;
            SeriesLength = model.Metadata.SeriesLength;
            WindowSize = model.Metadata.WindowSize;
            TrainedFromDate = model.Metadata.TrainedFromDate;
            TrainedToDate = model.Metadata.TrainedToDate;

            MeanForecastError = model.Metadata.MeanForecastError;
            MeanAbsoluteError = model.Metadata.MeanAbsoluteError;
            MeanSquaredError = model.Metadata.MeanSquaredError;

            // parse data from training file into a DateTimePoint series of one color.

            var testingData = File.ReadAllLines(model.TestingFilePath)
                .Skip(1)
                .Select(line =>
                {
                    var parts = line.Split(",");
                    var date = DateTime.Parse(parts[0]);
                    var feature = Convert.ToDouble(parts[1]);
                    return new DateTimePoint(date, feature);
                }).ToList();

            var trainingData = File.ReadAllLines(model.TrainingFilePath)
                .Skip(1)
                .Select(line =>
                {
                    var parts = line.Split(",");
                    var date = DateTime.Parse(parts[0]);
                    var feature = Convert.ToDouble(parts[1]);
                    return new DateTimePoint(date, feature);
                })
                .Where(trainingData =>  testingData.Find(pt => pt.DateTime.Equals(trainingData.DateTime)) == null);
            
            var forecastData = 
                File.ReadAllLines(model.TrainingForecastFilePath)
                .Skip(1)
                .Select(line =>
                {
                    var parts = line.Split(",");
                    var date = DateTime.Parse(parts[0]);
                    var feature = Convert.ToDouble(parts[1]);
                    var low = Convert.ToDouble(parts[2]);
                    var high = Convert.ToDouble(parts[3]);
                    return new FinancialPoint(date, high, feature - 0.1, feature, low);
                }).ToList();
            
            
            // parse data from testing file into a DateTimePoint series of another color.
            // parse data from training forecast file into a DateTime point series of another color,
            // that also highlights the error bounds.
            
            SeriesCollection = new ISeries[]
            {
                // Add all data to the series collection.
                new LineSeries<DateTimePoint>
                {
                    Name = "Training data",
                    Values = trainingData.TakeLast(forecastData.Count * 3).ToList(),
                    GeometryFill = new SolidColorPaint(SKColors.SkyBlue),
                    GeometrySize = 15,
                    Fill = null,
                    TooltipLabelFormatter = point =>
                    {
                        if (point.Model == null)
                            return "";
                        return $"Close: {point.Model.Value:F2} on {point.Model.DateTime.ToString("d")}";
                    }
                },
                new LineSeries<DateTimePoint>
                {
                    Name = "Testing data",
                    Values = testingData,
                    GeometryFill = new SolidColorPaint(SKColors.Violet),
                    Stroke = new SolidColorPaint(SKColors.DarkViolet) { StrokeThickness = 3 },
                    GeometryStroke = new SolidColorPaint(SKColors.DarkViolet),
                    GeometrySize = 15,
                    Fill = null,
                    TooltipLabelFormatter = point => $"Actual Close: {point.Model!.Value:F2} on {point.Model!.DateTime.ToString("d")}"
                }, 
                new CandlesticksSeries<FinancialPoint>
                {
                    Name = "Test forecast with error range",
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