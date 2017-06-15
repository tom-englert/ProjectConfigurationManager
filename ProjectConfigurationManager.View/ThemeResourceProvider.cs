namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.Windows;

    using TomsToolbox.Wpf.Styles;

    [Export(typeof(IThemeResourceProvider))]
    internal class ThemeResourceProvider : IThemeResourceProvider
    {
        public void LoadThemeResources(ResourceDictionary resource)
        {
        }
    }
}
