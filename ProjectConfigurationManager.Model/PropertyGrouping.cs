namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;

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
            "ApplicationIcon",
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
            if (name.StartsWith("CodeContracts", StringComparison.OrdinalIgnoreCase))
                return "CodeContracts";

            if (name.StartsWith("Scc"))
                return "Scc";

            if (_publishProperties.Contains(name))
                return "Publish";

            if (_signingProperties.Contains(name))
                return "Signing";

            if (name.Contains("CodeAnalysis"))
                return "CodeAnalysis";

            return "Common";
        }
    }
}
