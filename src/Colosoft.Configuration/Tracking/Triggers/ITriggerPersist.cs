using System;

namespace Colosoft.Configuration.Tracking.Triggers
{
    public interface ITriggerPersist
    {
        event EventHandler PersistRequired;
    }
}
