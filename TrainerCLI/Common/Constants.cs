using System;
using System.IO;

namespace CryptoForecaster.Common
{
    public static class Constants
    {
        public static readonly string DataDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\Data\"));
        public static readonly string RootDir = Path.GetFullPath(Path.Combine(DataDir, @"..\"));
        public static readonly string ModelsDir = Path.Combine(RootDir, @"Models\");
    }
}
