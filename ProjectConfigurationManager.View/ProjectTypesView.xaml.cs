namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Globalization;
    using System.Windows.Markup;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ProjectTypesView.xaml
    /// </summary>
    [DataTemplate(typeof(ProjectTypesViewModel))]
    public partial class ProjectTypesView
    {
        public ProjectTypesView()
        {
            InitializeComponent();
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }
    }
}
