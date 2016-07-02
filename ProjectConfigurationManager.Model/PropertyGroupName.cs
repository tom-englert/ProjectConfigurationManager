namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    public class PropertyGroupName : IEquatable<PropertyGroupName>
    {
        private readonly string _name;
        private readonly int _index;

        public PropertyGroupName(string name, int index)
        {
            Contract.Requires(name != null);

            _name = name;
            _index = index;
        }

        private static readonly HashSet<string> _projectSpecificProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProjectGuid",
            "OutputType",
            "AppDesignerFolder",
            "RootNamespace",
            "AssemblyName",
            "Tags"
        };

        private static readonly HashSet<string> _globalProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ApplicationIcon",
            "TargetFrameworkVersion",
            "TargetFrameworkProfile",
            "FileAlignment",
            "ProjectTypeGuids",
            "WarningLevel",
            "OutputPath",
            "DefineConstants",
            "PlatformTarget",
            "ErrorReport",
            "AllowUnsafeBlocks",
            "NoWarn",
            "TreatWarningsAsErrors",
            "WarningsAsErrors",
            "DocumentationFile",
            "GenerateSerializationAssemblies",
        };

        private static readonly HashSet<string> _debugProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "UseVSHostingProcess",
            "DebugSymbols",
            "DebugType",
            "Optimize",
        };

        private static readonly HashSet<string> _publishProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IsWebBootstrapper",
            "PublishUrl",
            "Install",
            "InstallFrom",
            "UpdateEnabled",
            "UpdateMode",
            "UpdateInterval",
            "UpdateIntervalUnits",
            "UpdatePeriodically",
            "UpdateRequired",
            "MapFileExtensions",
            "InstallUrl",
            "SupportUrl",
            "ProductName",
            "PublisherName",
            "ApplicationRevision",
            "ApplicationVersion",
            "UseApplicationTrust",
            "PublishWizardCompleted",
            "BootstrapperEnabled",
        };

        private static readonly HashSet<string> _signingProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SignAssembly",
            "AssemblyOriginatorKeyFile",
            "SignManifests",
            "ManifestCertificateThumbprint",
            "ManifestKeyFile",
            "GenerateManifests",
        };

        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _name;
            }
        }

        public int Index => _index;

        public static bool IsNotProjectSpecific(string propertyName)
        {
            return !_projectSpecificProperties.Contains(propertyName);
        }

        public static PropertyGroupName GetGroupForProperty(string propertyName)
        {
            Contract.Requires(propertyName != null);
            Contract.Ensures(Contract.Result<PropertyGroupName>() != null);

            if (_globalProperties.Contains(propertyName))
                return new PropertyGroupName("Global", 0);

            if (_debugProperties.Contains(propertyName))
                return new PropertyGroupName("Debug", 1);

            if (propertyName.StartsWith("CodeContracts", StringComparison.OrdinalIgnoreCase))
                return new PropertyGroupName("CodeContracts", 2);

            if (_publishProperties.Contains(propertyName))
                return new PropertyGroupName("Publish", 3);

            if (_signingProperties.Contains(propertyName))
                return new PropertyGroupName("Signing", 4);

            if (propertyName.StartsWith("Scc", StringComparison.OrdinalIgnoreCase))
                return new PropertyGroupName("Scc", 5);

            if (propertyName.Contains("CodeAnalysis"))
                return new PropertyGroupName("CodeAnalysis", 6);

            return new PropertyGroupName("Other", 7);
        }

        public override string ToString()
        {
            return _name;
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
            return _name.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as PropertyGroupName);
        }

        /// <summary>
        /// Determines whether the specified <see cref="PropertyGroupName"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="PropertyGroupName"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="PropertyGroupName"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(PropertyGroupName other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(PropertyGroupName left, PropertyGroupName right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return left.Name.Equals(right.Name);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(PropertyGroupName left, PropertyGroupName right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(PropertyGroupName left, PropertyGroupName right)
        {
            return !InternalEquals(left, right);
        }

        #endregion


    }
}
