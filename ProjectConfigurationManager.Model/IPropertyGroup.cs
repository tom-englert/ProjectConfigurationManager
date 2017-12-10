namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    internal interface IPropertyGroup
    {
        [NotNull, ItemNotNull]
        IEnumerable<IProjectProperty> Properties { get; }

        [CanBeNull]
        string ConditionExpression { get; }

        [CanBeNull]
        string Label { get; }

        void Delete();
    }

    internal interface IProjectPropertyGroup : IPropertyGroup
    {
        [NotNull]
        IProjectProperty AddProperty([NotNull] string propertyName);
    }

    internal interface IItemDefinitionGroup : IPropertyGroup
    {
        [NotNull]
        IProjectProperty AddProperty([NotNull] string groupName, [NotNull] string propertyName);
    }

}