namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof(IndexerContract<>))]
    public interface IIndexer<T>
    {
        T this[[NotNull] string key]
        {
            get;
            set;
        }
    }

    [ContractClassFor(typeof(IIndexer<>))]
    abstract class IndexerContract<T> : IIndexer<T>
    {
        public T this[string key]
        {
            get
            {
                Contract.Requires(key != null);
                throw new System.NotImplementedException();
            }
            set
            {
                Contract.Requires(key != null);
                throw new System.NotImplementedException();
            }
        }
    }
}