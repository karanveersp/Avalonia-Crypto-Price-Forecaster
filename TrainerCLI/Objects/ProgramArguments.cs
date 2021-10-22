using CryptoForecaster.Common;
using CryptoForecaster.Enums;
using System;
using System.IO;

namespace CryptoForecaster.Objects
{
    public class ProgramArguments
    {
        public ProgramActions Action { get; set; }
        public string Pair { get; set; }
        public string TrainingFileName { get; set; }
        public string ModelFileName { get; set; }

        public ProgramArguments()
        {
            Pair = "BTCUSD";
        }
    }
}
