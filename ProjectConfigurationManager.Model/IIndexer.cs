namespace tomenglertde.ProjectConfigurationManager.Model
{
    public interface IIndexer<T>
    {
        T this[string key]
        {
            get;
            set;
        }
    }
}