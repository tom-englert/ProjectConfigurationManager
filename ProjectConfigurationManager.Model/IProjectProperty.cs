namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Diagnostics.Contracts;

    [ContractClass(typeof (ProjectPropertyContract))]
    interface IProjectProperty
    {
        string Name { get; }

        string Value
        {
            get;
            set;
        }

        void Delete();
    }

    [ContractClassFor(typeof (IProjectProperty))]
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
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        public void Delete()
        {
            throw new System.NotImplementedException();
        }
    }
}
