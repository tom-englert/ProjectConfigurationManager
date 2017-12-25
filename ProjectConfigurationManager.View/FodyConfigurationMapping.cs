namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

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

            public int this[string weaverName]
            {
                get
                {
                    var weaverConfiguration = _viewModel.WeaverConfigurations.FirstOrDefault(configuration => configuration.Name == weaverName);
                    var projectConfiguration = weaverConfiguration?.Weavers.FirstOrDefault(weaver => weaver?.Project == _project)?.Configuration;
                    if (projectConfiguration == null)
                        return string.IsNullOrEmpty(weaverConfiguration?.Configuration["0"]) ? -1 : 0;

                    var weaverConfigurations = weaverConfiguration.Configurations;
                    var weaverProjectConfiguration = weaverConfigurations.LastOrDefault(c => c == projectConfiguration);
                    if (weaverProjectConfiguration == null)
                        return -1;

                    return weaverConfigurations.IndexOf(weaverProjectConfiguration);
                }
                set
                {
                    var currentValue = this[weaverName];

                    if (value == currentValue)
                        return;

                    if (value <= 0)
                    {
                        if (currentValue <= 0)
                            return;

                        SetWeaver(weaverName, _project, null);
                    }
                    else
                    {
                        var weaverConfiguration = _viewModel.WeaverConfigurations.FirstOrDefault(w => w.Name == weaverName);
                        var weaverConfigurations = weaverConfiguration?.Configurations;
                        if (weaverConfigurations == null)
                            return;

                        if (value >= weaverConfigurations.Count)
                            return;

                        var configuration = weaverConfigurations[value];

                        SetWeaver(weaverName, _project, configuration);
                    }
                }
            }

            private void SetWeaver([NotNull] string weaverName, [NotNull] Project project, [CanBeNull] string configuration)
            {
                var projectFolder = project.Folder;

                var document = FodyWeaver.LoadDocument(projectFolder);
                var root = document?.Root;
                if (root == null)
                {
                    document = XDocument.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?><Weavers/>");
                    root = document.Root;
                    Debug.Assert(root != null);
                }

                var weaverElements = root.Elements().ToArray();
                var weaverElement = weaverElements.FirstOrDefault(element => element?.Name.LocalName == weaverName);
                if (weaverElement == null)
                {
                    if (string.IsNullOrEmpty(configuration))
                        return;

                    root.Add(XElement.Parse(configuration));
                    _viewModel.SortWeavers(root);
                }
                else
                {
                    if (configuration != null)
                    {
                        weaverElement.AddAfterSelf(XElement.Parse(configuration));
                    }

                    weaverElement.Remove();
                }

                var fileName = FodyWeaver.SaveDocument(projectFolder, document);
                if (fileName != null && File.Exists(fileName))
                    project.AddFile(fileName);
            }
        }
    }
}