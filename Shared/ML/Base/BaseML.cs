
using Microsoft.ML;

namespace Shared.ML.Base
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