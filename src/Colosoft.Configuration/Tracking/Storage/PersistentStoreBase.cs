using System.Collections.Generic;

namespace Colosoft.Configuration.Tracking.Storage
{
    public abstract class PersistentStoreBase : IStore
    {
        private Dictionary<string, object> values;

        private Dictionary<string, object> Values
        {
            get
            {
                return this.values ?? (this.values = this.LoadValues());
            }
        }

        public bool ContainsKey(string key) => this.Values.ContainsKey(key);

        public void Set(string key, object value) => this.Values[key] = value;

        public object Get(string key) => this.Values[key];

        public void CommitChanges() => this.SaveValues(this.Values);

        protected abstract Dictionary<string, object> LoadValues();

        protected abstract void SaveValues(Dictionary<string, object> values);

        public void Clear()
        {
            this.values = new Dictionary<string, object>();
        }
    }
}
