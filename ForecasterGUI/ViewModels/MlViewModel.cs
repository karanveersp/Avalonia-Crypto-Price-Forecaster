using System.Diagnostics;
using Splat;

namespace ForecasterGUI.ViewModels
{
    public class MlViewModel : ViewModelBase
    {
        
        public ViewModelBase TrainerViewModel { get; private set; }
        public ViewModelBase PredictorViewModel { get; private set; }
        public ViewModelBase TrainingResultsViewModel { get; private set; }

        public MlViewModel()
        {
            TrainerViewModel = new TrainerViewModel();
            PredictorViewModel = new PredictorViewModel();
            TrainingResultsViewModel = new TrainingResultsViewModel();
            Trace.WriteLine($"MlViewModel initialized!");
        }
    }
}