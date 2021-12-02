using System.IO;
using Shared;

namespace ForecasterGUI.Models
{
    public class MlModel
    {
        public string DirPath { get; set; }
        public string Name { get; set; }
        public Shared.ML.Objects.ModelMetadata? Metadata { get; set; }
        
        public string TrainingFilePath { get; set; }
        public string TestingFilePath { get; set; }
        public string TrainingForecastFilePath { get; set; }

        public MlModel(string path)
        {
            Name = Path.GetFileName(path)!;
            DirPath = path;
            Metadata = Util.LoadModelMetadata(path);
            (TrainingFilePath, TestingFilePath, TrainingForecastFilePath) = Util.GetTrainingTestForecastPaths(path);
        }
    }
}