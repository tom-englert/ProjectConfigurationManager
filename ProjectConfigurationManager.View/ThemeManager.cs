namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Desktop;

    [Export]
    public sealed class ThemeManager : ObservableObject
    {
        public bool IsDarkTheme { get; set; }
    }
}
