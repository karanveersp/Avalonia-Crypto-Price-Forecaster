using CryptoForecaster.Common;
using CryptoForecaster.Helpers;
using CryptoForecaster.ML;
using CryptoForecaster.Objects;
using System;

namespace CryptoForecaster
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.Clear();

            var arguments = CommandLineParser.ParseArguments<ProgramArguments>(args);

            arguments.TrainingFileName = Constants.DataDir + $"{arguments.Pair}.csv";

            arguments.ModelFileName = Constants.ModelsDir + $"{arguments.Pair}_predictor.zip";

            switch (arguments.Action)
            {
                case Enums.ProgramActions.PREDICT:
                    new Predictor().Predict(arguments);
                    break;
                case Enums.ProgramActions.TRAINING:
                    new Trainer().Train(arguments);
                    break;
                default:
                    Console.WriteLine($"Unhandled action {arguments.Action}");
                    break;
            }
        }
    }
}
