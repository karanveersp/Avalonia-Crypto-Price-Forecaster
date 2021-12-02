using System.Collections.Generic;
using System.Diagnostics;
using Shared;

namespace ForecasterGUI.ViewModels
{
    public class HistoricalChartsViewModel : ViewModelBase
    {
        public ViewModelBase PercentChangesViewModel { get; private set; }
        public ViewModelBase ClosingPricesViewModel { get; private set; }
        // public ViewModelBase FinancialSeriesViewModel { get; private set; }

        public HistoricalChartsViewModel()
        {
            PercentChangesViewModel = new PercentChangesViewModel();
            ClosingPricesViewModel = new ClosingPricesViewModel();
            Trace.WriteLine($"HistoricalChartsViewModel initialized!");
        }
    }
}