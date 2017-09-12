namespace tomenglertde.ProjectConfigurationManager.Model
{
    using JetBrains.Annotations;

    public interface IIndexer<T>
    {
        [CanBeNull]
        T this[[NotNull] string key]
        {
            get;
            set;
        }
    }
}