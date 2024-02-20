using System;

namespace Colosoft.Configuration.Tracking
{
    public class TrackedPropertyInfo
    {
        public Func<object, object> Getter { get; }

        public Action<object, object> Setter { get; }

        public bool IsDefaultSpecified { get; }

        public object DefaultValue { get; }

        internal TrackedPropertyInfo(Func<object, object> getter, Action<object, object> setter)
            : this(getter, setter, false, null)
        {
        }

        internal TrackedPropertyInfo(Func<object, object> getter, Action<object, object> setter, bool isDefaultSpecified, object defaultValue)
        {
            this.Getter = getter;
            this.Setter = setter;
            this.IsDefaultSpecified = isDefaultSpecified;
            this.DefaultValue = defaultValue;
        }
    }
}
