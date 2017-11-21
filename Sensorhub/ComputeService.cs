using Google.Apis.Services;

namespace Sensorhub
{
    //this class is not used
    internal class ComputeService
    {
        private BaseClientService.Initializer initializer;

        public ComputeService(BaseClientService.Initializer initializer)
        {
            this.initializer = initializer;
        }

        public static object Scope { get; internal set; }
    }
}