namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof(ProjectPropertyContract))]
    interface IProjectProperty
    {
        [NotNull]
        string Name { get; }

        [NotNull]
        string Value
        {
            get;
            set;
        }

        void Delete();
    }

    [ContractClassFor(typeof(IProjectProperty))]
    abstract class ProjectPropertyContract : IProjectProperty
    {
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                throw new System.NotImplementedException();
            }
        }

        public string Value
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                throw new System.NotImplementedException();
            }
            set
            {
                Contract.Requires(value != null);
                throw new System.NotImplementedException();
            }
        }

        public void Delete()
        {
            throw new System.NotImplementedException();
        }
    }
}
