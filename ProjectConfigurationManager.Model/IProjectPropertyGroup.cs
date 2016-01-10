namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof (ProjectPropertyGroupContract))]
    interface IProjectPropertyGroup
    {
        IEnumerable<IProjectProperty> Properties { get; }

        IProjectProperty AddProperty(string propertyName);
    }

    [ContractClassFor(typeof (IProjectPropertyGroup))]
    abstract class ProjectPropertyGroupContract : IProjectPropertyGroup
    {
        public IEnumerable<IProjectProperty> Properties
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IProjectProperty>>() != null);
                throw new System.NotImplementedException();
            }
        }

        public IProjectProperty AddProperty(string propertyName)
        {
            Contract.Requires(propertyName != null);
            throw new System.NotImplementedException();
        }
    }
}
