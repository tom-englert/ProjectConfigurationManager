namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof (ProjectPropertyGroupContract))]
    internal interface IProjectPropertyGroup
    {
        [NotNull, ItemNotNull]
        IEnumerable<IProjectProperty> Properties { get; }

        string ConditionExpression { get; }

        [NotNull]
        IProjectProperty AddProperty([NotNull] string propertyName);

        void Delete();
    }

    [ContractClassFor(typeof (IProjectPropertyGroup))]
    internal abstract class ProjectPropertyGroupContract : IProjectPropertyGroup
    {
        public IEnumerable<IProjectProperty> Properties
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IProjectProperty>>() != null);
                throw new System.NotImplementedException();
            }
        }

        public abstract string ConditionExpression { get; }

        public IProjectProperty AddProperty(string propertyName)
        {
            Contract.Requires(propertyName != null);
            Contract.Ensures(Contract.Result<IProjectProperty>() != null);
            throw new System.NotImplementedException();
        }
        public abstract void Delete();
    }
}
