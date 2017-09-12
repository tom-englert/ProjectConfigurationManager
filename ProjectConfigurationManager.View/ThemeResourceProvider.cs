namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.Windows;

    using TomsToolbox.Wpf.Styles;

    [Export(typeof(IThemeResourceProvider))]
    internal sealed class ThemeResourceProvider : IThemeResourceProvider
    {
        public void LoadThemeResources(ResourceDictionary resource)
        {
        }
    }
}
