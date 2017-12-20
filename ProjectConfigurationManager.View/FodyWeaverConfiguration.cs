namespace tomenglertde.ProjectConfigurationManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    public sealed class FodyWeaverConfiguration : INotifyPropertyChanged
    {
        public FodyWeaverConfiguration([NotNull] string name, [NotNull, ItemNotNull] ICollection<FodyWeaver> weavers)
        {
            var solutionConfigurations = weavers
                .Where(w => w.Project == null)
                .Take(1)
                .Select(w => w.Configuration)
                .DefaultIfEmpty();

            var projectConfigurations = weavers
                .Where(w => w.Project != null)
                .Select(w => w.Configuration)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            Configurations = solutionConfigurations
                .Concat(projectConfigurations)
                .ToArray();

            Name = name;
            Weavers = weavers;

            Configuration = new ConfigurationIndexer(Configurations);
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public ICollection<FodyWeaver> Weavers { get; }

        [NotNull]
        public IIndexer<string> Configuration { get; }

        [NotNull]
        public IList<string> Configurations { get; }

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

        private sealed class ConfigurationIndexer : IIndexer<string>
        {
            [NotNull]
            private readonly IList<string> _configurations;

            public ConfigurationIndexer([NotNull] IList<string> configurations)
            {
                _configurations = configurations;
            }

            public string this[string key]
            {
                get
                {
                    if (!int.TryParse(key, out var index))
                        return string.Empty;

                    if (index < 0 || index >= _configurations.Count)
                        return string.Empty;

                    return _configurations[index];
                }
                set
                {
                }
            }
        }
    }
}