using System;

namespace Colosoft.Configuration.Tracking
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TrackingKeyAttribute : Attribute
    {
    }
}
