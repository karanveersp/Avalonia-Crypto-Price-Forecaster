
using Microsoft.ML;

namespace CryptoForecaster.ML.Base
{
    public class BaseML
    {
        protected readonly MLContext MlContext;

        protected BaseML()
        {
            MlContext = new MLContext(1);
        }
    }
}