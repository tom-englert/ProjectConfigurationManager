namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.ComponentModel;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    internal sealed class FodyConfigurationMapping : INotifyPropertyChanged
    {
        public FodyConfigurationMapping([NotNull] FodyViewModel viewModel, [NotNull] Project project)
        {
            Project = project;
            Configuration = new ConfigurationIndexer(viewModel, project);
        }

        [NotNull]
        public Project Project { get; }

        [NotNull, UsedImplicitly]
        public IIndexer<int> Configuration { get; }

        public void Update()
        {
            OnPropertyChanged(nameof(Configuration));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class ConfigurationIndexer : IIndexer<int>
        {
            [NotNull]
            private readonly FodyViewModel _viewModel;
            [NotNull]
            private readonly Project _project;

            public ConfigurationIndexer([NotNull] FodyViewModel viewModel, [NotNull] Project project)
            {
                _viewModel = viewModel;
                _project = project;
            }

            public int this[string weaver]
            {
                get
                {
                    var weaverConfiguration = _viewModel.WeaverConfigurations.FirstOrDefault(w => w.Name == weaver);
                    var projectConfiguration = weaverConfiguration?.Weavers.FirstOrDefault(w => w.Project == _project)?.Configuration;
                    if (projectConfiguration == null)
                        return string.IsNullOrEmpty(weaverConfiguration?.Configuration["0"]) ? -1 : 0;

                    var weaverConfigurations = weaverConfiguration.Configurations;
                    var weaverProjectConfiguration = weaverConfigurations.LastOrDefault(c => c == projectConfiguration);
                    if (weaverProjectConfiguration == null)
                        return -1;

                    return weaverConfigurations.IndexOf(weaverProjectConfiguration);
                }
                set => throw new NotImplementedException();
            }
        }
    }
}