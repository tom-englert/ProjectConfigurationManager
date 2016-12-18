namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using TomsToolbox.Desktop;

    [Export]
    public class Configuration : ObservableObjectBase
    {
        [NotNull]
        private readonly ITracer _tracer;
        private const string FileName = "config.json";
        private static readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tom-englert.de", "ProjectConfigurationManager");

        private readonly string _filePath;
        private IDictionary<string, string[]> _propertyColumnOrder;

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
        [NotNull]
        public IDictionary<string, string[]> PropertyColumnOrder
        {
            get
            {
                Contract.Ensures(Contract.Result<IDictionary<string, string[]>>() != null);
                return _propertyColumnOrder ?? (_propertyColumnOrder = new Dictionary<string, string[]>());
            }
            set
            {
                SetProperty(ref _propertyColumnOrder, value);
            }
        }

        public void Save()
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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
        }
    }
}

