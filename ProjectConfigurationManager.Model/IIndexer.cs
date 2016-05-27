namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Diagnostics.Contracts;

    [ContractClass(typeof(IndexerContract<>))]
    public interface IIndexer<T>
    {
        T this[string key]
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