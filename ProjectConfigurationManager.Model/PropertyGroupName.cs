namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [Equals]
    public class PropertyGroupName
    {
        public PropertyGroupName([NotNull] string name, int index)
        {
            Contract.Requires(name != null);

            Name = name;
            Index = index;
        }

        [NotNull]
        private static readonly HashSet<string> _projectSpecificProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProjectGuid",
            "OutputType",
            "AppDesignerFolder",
            "RootNamespace",
            "AssemblyName",
            "Tags"
        };

        [NotNull]
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

        [NotNull]
        private static readonly HashSet<string> _debugProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "UseVSHostingProcess",
            "DebugSymbols",
            "DebugType",
            "Optimize",
        };

        [NotNull]
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

        [NotNull]
        private static readonly HashSet<string> _signingProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SignAssembly",
            "AssemblyOriginatorKeyFile",
            "SignManifests",
            "ManifestCertificateThumbprint",
            "ManifestKeyFile",
            "GenerateManifests",
        };

        [NotNull]
        public string Name { get; }

        [IgnoreDuringEquals]
        public int Index { get; }

        public static bool IsNotProjectSpecific([CanBeNull] string propertyName)
        {
            return _projectSpecificProperties.Contains(propertyName) != true;
        }

        [NotNull]
        public static PropertyGroupName GetGroupForProperty([NotNull] string propertyName)
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
            return Name;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Name != null);
        }

    }
}
