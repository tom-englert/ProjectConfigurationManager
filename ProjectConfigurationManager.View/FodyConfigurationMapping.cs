namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    public sealed class FodyConfigurationMapping : INotifyPropertyChanged
    {
        public FodyConfigurationMapping([NotNull] Project project, [NotNull] ICollection<FodyWeaverConfiguration> weaverConfigurations)
        {
            Project = project;
            Configuration = new ConfigurationIndexer(project, weaverConfigurations);
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
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class ConfigurationIndexer : IIndexer<int>
        {
            [NotNull]
            private readonly ICollection<FodyWeaverConfiguration> _weaverConfigurations;
            [NotNull]
            private readonly Project _project;

            public ConfigurationIndexer([NotNull] Project project, [NotNull] ICollection<FodyWeaverConfiguration> weaverConfigurations)
            {
                _weaverConfigurations = weaverConfigurations;
                _project = project;
            }

            public int this[string weaver]
            {
                get
                {
                    var weaverConfiguration = _weaverConfigurations.FirstOrDefault(w => w.Name == weaver);
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