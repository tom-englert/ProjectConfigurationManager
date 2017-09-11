namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;

    using TomsToolbox.Desktop;

    [Export]
    public class ThemeManager : ObservableObject
    {
        public bool IsDarkTheme { get; set; }
    }
}
