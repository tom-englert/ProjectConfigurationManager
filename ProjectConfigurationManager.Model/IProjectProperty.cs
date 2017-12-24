namespace tomenglertde.ProjectConfigurationManager.Model
{
    using JetBrains.Annotations;

    internal interface IProjectProperty
    {
        [NotNull]
        string Name { get; }

        [NotNull]
        IPropertyGroup Group { get; }

        [NotNull]
        string Value
        {
            get;
            set;
        }

        void Delete();
    }
}
