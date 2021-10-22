using CryptoForecaster.ML.Base;
using CryptoForecaster.Objects;
using System.IO;
using System;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using CryptoForecaster.ML.Objects;

namespace CryptoForecaster.ML
{
    public class Predictor : BaseML
    {
        public void Predict(ProgramArguments args)
        {
            var modelFilePath = Path.Combine(AppContext.BaseDirectory, args.ModelFileName);

            if (!File.Exists(modelFilePath))
            {
                Console.WriteLine($"Failed to find model at {modelFilePath}");
                return;
            }

            ITransformer mlModel;

            using (var stream = new FileStream(modelFilePath,
                FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                mlModel = MlContext.Model.Load(stream, out _);
            } 

            if (mlModel == null)
            {
                Console.WriteLine("Failed to load model");
                return;
            }

            var predictionEngine = mlModel.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);

            var prediction = predictionEngine.Predict();
            prediction.PrintForecastValuesAndIntervals();
        }
    }
}