using System;

namespace Colosoft.Configuration.Tracking
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class TrackableAttribute : Attribute
    {
        public string TrackerName { get; }

        public bool IsDefaultSpecified { get; }

        public object DefaultValue { get; private set; }

        public TrackableAttribute()
        {
        }

        public TrackableAttribute(string trackerName)
        {
            this.TrackerName = trackerName;
        }

        public TrackableAttribute(string trackerName, object defaultValue)
        {
            this.TrackerName = trackerName;
            this.IsDefaultSpecified = true;
            this.DefaultValue = defaultValue;
        }
    }
}
