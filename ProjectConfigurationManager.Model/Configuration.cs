namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    [Export]
    public sealed class Configuration : INotifyPropertyChanged
    {
        private const string FileName = "config.json";

        [NotNull]
        private readonly ITracer _tracer;
        [NotNull]
        private static readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ProjectConfigurationManager");

        [NotNull]
        private readonly string _filePath;

        [ImportingConstructor]
        public Configuration([NotNull] ITracer tracer)
        {
            Contract.Requires(tracer != null);
            Contract.Assume(!string.IsNullOrEmpty(_directory));

            _tracer = tracer;
            _filePath = Path.Combine(_directory, FileName);

            try
            {
                Directory.CreateDirectory(_directory);

                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    JsonConvert.PopulateObject(json, this);
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }

            PropertyChanged += (_, __) => Save();
        }

        /// <summary>
        /// Gets or sets the property column order by group name.
        /// </summary>
        /// <value>
        /// The property column order, i.e. all column names by group in display order.
        /// </value>
        [CanBeNull]
        public ImmutableDictionary<string, string[]> PropertyColumnOrder { get; set; }

        private void Save()
        {
            try
            {
                File.WriteAllText(_filePath, JsonConvert.SerializeObject(this));
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error writing configuration file: " + _filePath + " - " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [UsedImplicitly]
        private void OnPropertyChanged([NotNull] string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_filePath != null);
        }
    }
}

