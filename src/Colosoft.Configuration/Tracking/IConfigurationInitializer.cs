using System;

namespace Colosoft.Configuration.Tracking
{
    public interface IConfigurationInitializer
    {
        Type ForType { get; }

        void InitializeConfiguration(TrackingConfiguration configuration);
    }
}
