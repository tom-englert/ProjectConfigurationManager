namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    public static class PropertyGrouping
    {
        private static readonly HashSet<string> _projectSpecificProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProjectGuid",
            "OutputType",
            "AppDesignerFolder",
            "RootNamespace",
            "AssemblyName",
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

        public static bool IsNotProjectSpecific(string propertyName)
        {
            return !_projectSpecificProperties.Contains(propertyName);
        }

        public static string GetPropertyGroupName(string name)
        {
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (_globalProperties.Contains(name))
                return "Global";

            if (_publishProperties.Contains(name))
                return "Publish";

            if (_signingProperties.Contains(name))
                return "Signing";

            if (_debugProperties.Contains(name))
                return "Debug";

            if (name.StartsWith("CodeContracts", StringComparison.OrdinalIgnoreCase))
                return "CodeContracts";

            if (name.StartsWith("Scc"))
                return "Scc";

            if (name.Contains("CodeAnalysis"))
                return "CodeAnalysis";

            return "Other";
        }
    }
}
