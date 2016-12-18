namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof (ProjectPropertyGroupContract))]
    interface IProjectPropertyGroup
    {
        [NotNull]
        IEnumerable<IProjectProperty> Properties { get; }

        IProjectProperty AddProperty([NotNull] string propertyName);
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
