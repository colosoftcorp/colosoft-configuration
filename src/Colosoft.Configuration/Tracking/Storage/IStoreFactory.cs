namespace Colosoft.Configuration.Tracking.Storage
{
    public interface IStoreFactory
    {
        IStore CreateStoreForObject(string objectId);
    }
}
