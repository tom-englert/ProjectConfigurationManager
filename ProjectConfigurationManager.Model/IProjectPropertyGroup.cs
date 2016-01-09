namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof (ProjectPropertyGroupContract))]
    interface IProjectPropertyGroup
    {
        IEnumerable<IProjectProperty> Properties { get; }
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
    }
}
