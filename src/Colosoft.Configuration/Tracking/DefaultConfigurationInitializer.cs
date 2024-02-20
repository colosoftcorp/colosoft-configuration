using System;
using System.Linq;

namespace Colosoft.Configuration.Tracking
{
    public class DefaultConfigurationInitializer : IConfigurationInitializer
    {
        public virtual Type ForType => typeof(object);

        public virtual void InitializeConfiguration(TrackingConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            object target = configuration.TargetReference.Target;

            Type targetType = target.GetType();
            var keyProperty = targetType.GetProperties().SingleOrDefault(pi => pi.IsDefined(typeof(TrackingKeyAttribute), true));
            if (keyProperty != null)
            {
                configuration.Key = keyProperty.GetValue(target, null).ToString();
            }

            foreach (var pi in targetType.GetProperties())
            {
                var propTrackableAtt = pi.GetCustomAttributes(true)
                    .OfType<TrackableAttribute>()
                    .SingleOrDefault(ta => ta.TrackerName == configuration.StateTracker.Name);

                if (propTrackableAtt != null)
                {
                    if (propTrackableAtt.IsDefaultSpecified)
                    {
                        configuration.AddProperty(pi.Name, propTrackableAtt.DefaultValue);
                    }
                    else
                    {
                        configuration.AddProperty(pi.Name);
                    }
                }
            }

            var trackingAwareTarget = target as ITrackingAware;
            if (trackingAwareTarget != null)
            {
                trackingAwareTarget.InitConfiguration(configuration);
            }
        }
    }
}
