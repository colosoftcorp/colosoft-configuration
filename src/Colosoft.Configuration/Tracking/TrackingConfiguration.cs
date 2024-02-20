using Colosoft.Configuration.Tracking.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Colosoft.Configuration.Tracking
{
    public sealed class TrackingConfiguration
    {
        public bool IsApplied { get; private set; }

        public StateTracker StateTracker { get; }

        public IStore TargetStore { get; private set; }

        public WeakReference TargetReference { get; private set; }

        public string Key { get; set; }

        public NamingScheme StoreNamingScheme { get; set; } = NamingScheme.TypeNameAndKey;

        public Dictionary<string, TrackedPropertyInfo> TrackedProperties { get; } = new Dictionary<string, TrackedPropertyInfo>();

        public bool AutoPersistEnabled { get; set; } = true;

        public event EventHandler<TrackingOperationEventArgs> ApplyingProperty;

        public event EventHandler StateApplied;

        public event EventHandler<TrackingOperationEventArgs> PersistingProperty;

        public event EventHandler StatePersisted;

        private bool OnApplyingState(string property, ref object value)
        {
            var handler = this.ApplyingProperty;
            if (handler != null)
            {
                var args = new TrackingOperationEventArgs(this, property, value);
                handler(this, args);
                value = args.Value;
                return !args.Cancel;
            }
            else
            {
                return true;
            }
        }

        private void OnStateApplied()
        {
            this.StateApplied?.Invoke(this, EventArgs.Empty);
        }

        private bool OnPersistingState(string property, ref object value)
        {
            var handler = this.PersistingProperty;
            if (handler != null)
            {
                var args = new TrackingOperationEventArgs(this, property, value);
                handler(this, args);
                value = args.Value;
                return !args.Cancel;
            }
            else
            {
                return true;
            }
        }

        private void OnStatePersisted()
        {
            this.StatePersisted?.Invoke(this, EventArgs.Empty);
        }

        internal TrackingConfiguration(object target, StateTracker tracker)
            : this(target, null, tracker)
        {
        }

        internal TrackingConfiguration(object target, string idenitifier, StateTracker tracker)
        {
            this.TargetReference = new WeakReference(target);
            this.Key = idenitifier;
            this.StateTracker = tracker;
        }

        private static string GetPropertyNameFromExpression<T>(Expression<Func<T, object>> exp)
        {
            MemberExpression membershipExpression;
            if (exp.Body is UnaryExpression)
            {
                membershipExpression = (exp.Body as UnaryExpression).Operand as MemberExpression;
            }
            else
            {
                membershipExpression = exp.Body as MemberExpression;
            }

            return membershipExpression.Member.Name;
        }

        private static IEnumerable<string> GetPropertyNamesFromExpression<T>(Expression<Func<T, object[]>> exp)
        {
            NewArrayExpression arrayExpression = exp.Body as NewArrayExpression;

            foreach (Expression subExp in arrayExpression.Expressions)
            {
                MemberExpression membershipExpression = null;
                if (subExp is UnaryExpression)
                {
                    membershipExpression = (subExp as UnaryExpression).Operand as MemberExpression;
                }
                else
                {
                    membershipExpression = subExp as MemberExpression;
                }

                yield return membershipExpression.Member.Name;
            }
        }

        public void Persist()
        {
            if (this.TargetReference.IsAlive)
            {
                if (this.TargetStore == null)
                {
                    this.TargetStore = this.InitStore();
                }

                // para impedir que quaisquer propriedades anteriormente existentes
                this.TargetStore.Clear();

                foreach (string propertyName in this.TrackedProperties.Keys)
                {
                    var value = this.TrackedProperties[propertyName].Getter(this.TargetReference.Target);
                    try
                    {
                        var shouldPersist = this.OnPersistingState(propertyName, ref value);
                        if (shouldPersist)
                        {
                            this.TargetStore.Set(propertyName, value);
                        }
                        else
                        {
                            Trace.WriteLine(ResourceMessageFormatter.Create(
                                () => Properties.Resources.TrackingConfiguration_PersistingCancelled,
                                this.Key,
                                propertyName).Format());
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ResourceMessageFormatter.Create(
                                () => Properties.Resources.TrackingConfiguration_PersistingFailed,
                                propertyName,
                                ex.Message).Format());
                    }
                }

                this.TargetStore.CommitChanges();

                this.OnStatePersisted();
            }
        }

        /// <summary>
        /// Aplica todos os dados armazenados anteriormente às propriedades rastreadas do objeto de destino.
        /// </summary>
        public void Apply()
        {
            if (this.TargetReference.IsAlive)
            {
                if (this.TargetStore == null)
                {
                    this.TargetStore = this.InitStore();
                }

                foreach (string propertyName in this.TrackedProperties.Keys)
                {
                    var descriptor = this.TrackedProperties[propertyName];

                    if (this.TargetStore.ContainsKey(propertyName))
                    {
                        try
                        {
                            object value = this.TargetStore.Get(propertyName);
                            var shouldApply = this.OnApplyingState(propertyName, ref value);
                            if (shouldApply)
                            {
                                descriptor.Setter(this.TargetReference.Target, value);
                            }
                            else
                            {
                                Trace.WriteLine(ResourceMessageFormatter.Create(
                                () => Properties.Resources.TrackingConfiguration_PersistingCancelled,
                                this.Key,
                                propertyName).Format());
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"TRACKING: Applying tracking to property with key='{propertyName}' failed. ExceptionType:'{ex.GetType().Name}', message: '{ex.Message}'!");
                        }
                    }
                    else if (descriptor.IsDefaultSpecified)
                    {
                        descriptor.Setter(this.TargetReference.Target, descriptor.DefaultValue);
                    }
                }

                this.OnStateApplied();
            }

            this.IsApplied = true;
        }

        public TrackingConfiguration IdentifyAs(string key, NamingScheme storeNamingScheme = NamingScheme.TypeNameAndKey)
        {
            if (this.TargetStore != null)
            {
                throw new InvalidOperationException("Can't set key after TargetStore has been set (which happens the first time Apply() or Persist() is called).");
            }

            this.Key = key;
            this.StoreNamingScheme = storeNamingScheme;

            return this;
        }

        public TrackingConfiguration AddProperties(params string[] properties)
        {
            foreach (string property in properties)
            {
                this.TrackedProperties[property] = this.CreateDescriptor(property, false, null);
            }

            return this;
        }

        public TrackingConfiguration AddProperties<T>(params Expression<Func<T, object>>[] properties)
        {
            this.AddProperties(properties.Select(p => GetPropertyNameFromExpression(p)).ToArray());
            return this;
        }

        public TrackingConfiguration AddProperties<T>(Expression<Func<T, object[]>> properties) // Addresses GitHub issue #23
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            this.AddProperties(GetPropertyNamesFromExpression(properties).ToArray());
            return this;
        }

        public TrackingConfiguration AddProperty<T>(Expression<Func<T, object>> property, object defaultValue)
        {
            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            this.AddProperty(GetPropertyNameFromExpression(property), defaultValue);
            return this;
        }

        public TrackingConfiguration AddProperty<T>(Expression<Func<T, object>> property)
        {
            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            this.AddProperty(GetPropertyNameFromExpression(property));
            return this;
        }

        public TrackingConfiguration AddProperty(string property, object defaultValue)
        {
            this.TrackedProperties[property] = this.CreateDescriptor(property, true, defaultValue);
            return this;
        }

        public TrackingConfiguration AddProperty(string property)
        {
            this.TrackedProperties[property] = this.CreateDescriptor(property, false, null);
            return this;
        }

        public TrackingConfiguration RemoveProperties(params string[] properties)
        {
            foreach (string property in properties)
            {
                this.TrackedProperties.Remove(property);
            }

            return this;
        }

        public TrackingConfiguration RemoveProperties<T>(params Expression<Func<T, object>>[] properties)
        {
            this.RemoveProperties(properties.Select(p => GetPropertyNameFromExpression(p)).ToArray());
            return this;
        }

        public TrackingConfiguration RegisterPersistTrigger(string eventName)
        {
            return this.RegisterPersistTrigger(eventName, this.TargetReference.Target);
        }

        public TrackingConfiguration RegisterPersistTrigger(string eventName, object eventSourceObject)
        {
            if (eventSourceObject is null)
            {
                throw new ArgumentNullException(nameof(eventSourceObject));
            }

            var eventInfo = eventSourceObject.GetType().GetEvent(eventName);
            var parameters = eventInfo.EventHandlerType
                .GetMethod("Invoke")
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType))
                .ToArray();

            var handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Call(
                        Expression.Constant(new Action(() =>
                        {
                            if (this.IsApplied)
                            {
                                this.Persist(); /*não persista antes de aplicar o valor armazenado*/
                            }
                        })),
                        "Invoke",
                        Type.EmptyTypes),
                    parameters)
              .Compile();

            eventInfo.AddEventHandler(eventSourceObject, handler);
            return this;
        }

        public TrackingConfiguration SetAutoPersistEnabled(bool shouldAutoPersist)
        {
            this.AutoPersistEnabled = shouldAutoPersist;
            return this;
        }

        private IStore InitStore()
        {
            object target = this.TargetReference.Target;

            string storeName;
            switch (this.StoreNamingScheme)
            {
                case NamingScheme.KeyOnly:
                    storeName = this.Key;
                    break;

                default:
                    storeName = this.Key == null ? target.GetType().Name : $"{target.GetType().Name}_{this.Key}";
                    break;
            }

            return this.StateTracker.StoreFactory.CreateStoreForObject(storeName);
        }

        private TrackedPropertyInfo CreateDescriptor(string propertyName, bool isDefaultSpecifier, object defaultValue)
        {
            var pi = this.TargetReference.Target.GetType().GetProperty(propertyName);
            return new TrackedPropertyInfo(
                obj => pi.GetValue(obj, null),
                (obj, val) => pi.SetValue(obj, val, null),
                isDefaultSpecifier,
                defaultValue);
        }
    }
}
