namespace Colosoft.Configuration.Tracking.Storage
{
    public interface IStore
    {
        bool ContainsKey(string key);

        void Set(string key, object value);

        object Get(string key);

        void CommitChanges();

        void Clear();
    }
}
