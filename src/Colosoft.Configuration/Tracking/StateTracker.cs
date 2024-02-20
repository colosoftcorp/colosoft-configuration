using Colosoft.Configuration.Tracking.Storage;
using Colosoft.Configuration.Tracking.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Colosoft.Configuration.Tracking
{
    public class StateTracker
    {
        private readonly ConditionalWeakTable<object, TrackingConfiguration> configurations = new ConditionalWeakTable<object, TrackingConfiguration>();

        private readonly List<WeakReference> trackedObjects = new List<WeakReference>();

        private ITriggerPersist autoPersistTrigger;

        public string Name { get; set; }

        public IStoreFactory StoreFactory { get; set; }

        public Dictionary<Type, IConfigurationInitializer> ConfigurationInitializers { get; } = new Dictionary<Type, IConfigurationInitializer>();

        public ITriggerPersist AutoPersistTrigger
        {
            get { return this.autoPersistTrigger; }
            set
            {
                if (this.autoPersistTrigger != null)
                {
                    this.AutoPersistTrigger.PersistRequired -= this.AutoPersistTriggerPersistRequired;
                }

                this.autoPersistTrigger = value;
                this.autoPersistTrigger.PersistRequired += this.AutoPersistTriggerPersistRequired;
            }
        }

        public StateTracker(IStoreFactory storeFactory, ITriggerPersist persistTrigger)
        {
            this.StoreFactory = storeFactory;
            this.AutoPersistTrigger = persistTrigger;

            this.RegisterConfigurationInitializer(new DefaultConfigurationInitializer()); // o padrão, será usado para todos os objetos que não tiverem um inicializador mais específico
        }

        public void RegisterConfigurationInitializer(IConfigurationInitializer configurationInitializer)
        {
            if (configurationInitializer is null)
            {
                throw new ArgumentNullException(nameof(configurationInitializer));
            }

            this.ConfigurationInitializers[configurationInitializer.ForType] = configurationInitializer;
        }

        private void AutoPersistTriggerPersistRequired(object sender, EventArgs e)
        {
            this.RunAutoPersist();
        }

        public TrackingConfiguration Configure(object target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var config = this.FindExistingConfig(target);

            if (config == null)
            {
                config = new TrackingConfiguration(target, this);
                var initializer = this.FindInitializer(target.GetType());
                initializer.InitializeConfiguration(config);
                this.trackedObjects.Add(new WeakReference(target));
                this.configurations.Add(target, config);
            }

            return config;
        }

        private IConfigurationInitializer FindInitializer(Type type)
        {
            IConfigurationInitializer initializer = this.ConfigurationInitializers.ContainsKey(type) ? this.ConfigurationInitializers[type] : null;

            if (initializer != null || type == typeof(object))
            {
                return initializer;
            }
            else
            {
                return this.FindInitializer(type.BaseType);
            }
        }

        public void RunAutoPersist()
        {
            foreach (var target in this.trackedObjects.Where(o => o.IsAlive).Select(o => o.Target))
            {
                if (this.configurations.TryGetValue(target, out var configuration) && configuration.AutoPersistEnabled)
                {
                    configuration.Persist();
                }
            }
        }

        private TrackingConfiguration FindExistingConfig(object target)
        {
            this.configurations.TryGetValue(target, out var configuration);
            return configuration;
        }
    }
}
