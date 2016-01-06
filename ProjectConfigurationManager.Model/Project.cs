namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Linq;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    public class Project : ObservableObject, IEquatable<Project>
    {
        private readonly Solution _solution;
        private readonly EnvDTE.Project _project;
        private readonly ObservableCollection<ProjectConfiguration> _internalProjectConfigurations = new ObservableCollection<ProjectConfiguration>();
        private readonly ReadOnlyObservableCollection<ProjectConfiguration> _projectConfigurations;
        private readonly IObservableCollection<SolutionContext> _solutionContexts;

        internal Project(Solution solution, EnvDTE.Project project)
        {
            _solution = solution;
            _project = project;
            _projectConfigurations = new ReadOnlyObservableCollection<ProjectConfiguration>(_internalProjectConfigurations);
            _solutionContexts = _solution.SolutionContexts.ObservableWhere(ctx => ctx.ProjectName == UniqueName);

            Update();

            //using Microsoft.VisualStudio.Shell;
            //using Microsoft.VisualStudio.Shell.Interop;

            //try
            //{
            //    var uniqueName = project.UniqueName;
            //    var vsSolution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));

            //    IVsHierarchy hierarchy;
            //    vsSolution.GetProjectOfUniqueName(uniqueName, out hierarchy);
            //    var buildPropertyStorage = hierarchy as IVsBuildPropertyStorage;
            //    var projectBuildSystem = hierarchy as IVsProjectBuildSystem;

            //    if (buildPropertyStorage != null)
            //    {
            //        bool success;
            //        var hr = projectBuildSystem.BuildTarget("Any CPU", out success);

            //        string value;
            //        hr = buildPropertyStorage.GetPropertyValue("WarningLevel", "Debug|AnyCPU", (int)_PersistStorageType.PST_PROJECT_FILE, out value);
            //        hr = buildPropertyStorage.GetPropertyValue("WarningLevel", "Release|AnyCPU", (int)_PersistStorageType.PST_PROJECT_FILE, out value);
            //        hr = buildPropertyStorage.GetPropertyValue("WarningLevel", "Release|Any CPU", (int)_PersistStorageType.PST_PROJECT_FILE, out value);
            //        hr = buildPropertyStorage.GetPropertyValue("WarningLevel", "Release", (int)_PersistStorageType.PST_PROJECT_FILE, out value);
            //    }

            //    //var buildProject = new Microsoft.Build.Evaluation.Project(_project.FullName, _defaults, null);
            //    //var properties = buildProject.AllEvaluatedProperties.Where(p => !p.IsEnvironmentProperty && !p.IsGlobalProperty && !p.IsImported && !p.IsReservedProperty).ToArray();
            //    //buildProject.SetProperty("WarningLevel", "3");
            //    //buildProject.Save();
            //}
            //catch (NotImplementedException)
            //{
            //}
        }

        public Solution Solution => _solution;

        public string Name => _project.Name;

        public string UniqueName => _project.UniqueName;

        public string[] PropertyNames => GetPropertyNames();

        public IObservableCollection<SolutionContext> SolutionContexts => _solutionContexts;

        public ReadOnlyObservableCollection<ProjectConfiguration> ProjectConfigurations => _projectConfigurations;

        internal void Update()
        {
            var configurationManager = _project.ConfigurationManager;

            if (configurationManager != null)
            {
                var configurationNames = ((IEnumerable)configurationManager.ConfigurationRowNames)?.OfType<string>();
                var platformNames = ((IEnumerable)configurationManager.PlatformNames)?.OfType<string>();

                if ((configurationNames != null) && (platformNames != null))
                {
                    _internalProjectConfigurations.SynchronizeWith((configurationNames.SelectMany(cn => platformNames.Select(pn => new ProjectConfiguration(this, cn, pn))).ToArray()));
                    return;
                }
            }

            _internalProjectConfigurations.Clear();
        }

        private string[] GetPropertyNames()
        {
            try
            {
                return _project.ConfigurationManager
                    .OfType<EnvDTE.Configuration>()
                    .SelectMany(conf => conf.Properties.GetProperties().Select(prop => prop.Name))
                    .Distinct()
                    .ToArray();
            }
            catch
            {
                return new string[0];
            }
        }

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _project.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Project);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Project"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Project"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="Project"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(Project other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(Project left, Project right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return left._project == right._project;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(Project left, Project right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(Project left, Project right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
