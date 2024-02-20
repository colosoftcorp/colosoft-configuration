using System.ComponentModel;

namespace Colosoft.Configuration.Tracking
{
    public class TrackingOperationEventArgs : CancelEventArgs
    {
        public TrackingConfiguration Configuration { get; }

        public string Property { get; set; }

        public object Value { get; set; }

        public TrackingOperationEventArgs(TrackingConfiguration configuration, string property, object value)
        {
            this.Configuration = configuration;
            this.Property = property;
            this.Value = value;
        }
    }
}
