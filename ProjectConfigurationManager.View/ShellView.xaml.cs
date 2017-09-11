namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Media;

    using JetBrains.Annotations;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    [Export]
    public partial class ShellView
    {
        [NotNull]
        private readonly ThemeManager _themeManager;

        [ImportingConstructor]
        public ShellView([NotNull] ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);
            this.SetExportProvider(exportProvider);

            _themeManager = exportProvider.GetExportedValue<ThemeManager>();

            InitializeComponent();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if ((e.Property != ForegroundProperty) && (e.Property != BackgroundProperty))
                return;

            var foreground = ToGray((Foreground as SolidColorBrush)?.Color);
            var background = ToGray((Background as SolidColorBrush)?.Color);

            _themeManager.IsDarkTheme = background < foreground;
        }


        private static double ToGray(Color? color)
        {
            return color?.R * 0.21 + color?.G * 0.72 + color?.B * 0.07 ?? 0.0;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_themeManager != null);
        }
    }
}
