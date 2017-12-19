namespace tomenglertde.ProjectConfigurationManager.View.Themes
{
    using System.Windows;

    using JetBrains.Annotations;

    public static class ResourceKeys
    {
        [NotNull]
        public static readonly ResourceKey TagFilterTemplate = new ComponentResourceKey(typeof(ResourceKeys), "TagFilterTemplate");


        [NotNull]
        public static readonly ResourceKey MultipleChoiceFilterTemplate = new ComponentResourceKey(typeof(ResourceKeys), "MultipleChoiceFilterTemplate");


        [NotNull]
        public static readonly ResourceKey ProjectNameTemplate = new ComponentResourceKey(typeof(ResourceKeys), "ProjectNameTemplate");


        [NotNull]
        public static readonly ResourceKey ProjectConfigurationNameTemplate = new ComponentResourceKey(typeof(ResourceKeys), "ProjectConfigurationNameTemplate");

        [NotNull]
        public static readonly ResourceKey FodyConfigurationMappingNameTemplate = new ComponentResourceKey(typeof(ResourceKeys), "FodyConfigurationMappingNameTemplate");
    }
}
