namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell.Interop;

    internal static class ExtensionMethods
    {
        public static void RemoveSelfAndWhitespace([NotNull] this XElement element)
        {
            Contract.Requires(element != null);

            while (true)
            {
                var previous = element.PreviousNode as XText;

                if ((previous != null) && string.IsNullOrWhiteSpace(previous.Value))
                {
                    previous.Remove();
                }

                var parent = element.Parent;

                element.Remove();

                if ((parent == null) || parent.HasElements)
                    return;

                element = parent;
            }
        }

        public static bool MatchesConfiguration([NotNull] this IProjectPropertyGroup propertyGroup, string configuration, string platform)
        {
            Contract.Requires(propertyGroup != null);

            var conditionExpression = propertyGroup.ConditionExpression;

            if (string.IsNullOrEmpty(conditionExpression))
                return string.IsNullOrEmpty(configuration) && string.IsNullOrEmpty(platform);

            string groupConfiguration;
            string groupPlatform;

            return conditionExpression.ParseCondition(out groupConfiguration, out groupPlatform)
                   && configuration == groupConfiguration
                   && platform == groupPlatform;
        }

        [NotNull, ItemNotNull]
        internal static IEnumerable<ProjectConfiguration> GetProjectConfigurations([NotNull] this Project project)
        {
            Contract.Requires(project != null);
            Contract.Ensures(Contract.Result<IEnumerable<ProjectConfiguration>>() != null);

            var configurationNames = new HashSet<string>();
            var platformNames = new HashSet<string>();

            var projectFile = project.ProjectFile;

            ParseConfigurations(projectFile, configurationNames, platformNames);

            var projectConfigurations = configurationNames
                .SelectMany(configuration => platformNames.Select(platform => new ProjectConfiguration(project, configuration, platform)));

            return projectConfigurations;
        }

        private static void ParseConfigurations([NotNull] this ProjectFile projectFile, [NotNull] ICollection<string> configurationNames, [NotNull] ICollection<string> platformNames)
        {
            Contract.Requires(projectFile != null);
            Contract.Requires(configurationNames != null);
            Contract.Requires(platformNames != null);

            foreach (var propertyGroup in projectFile.PropertyGroups)
            {
                Contract.Assume(propertyGroup != null);

                var conditionExpression = propertyGroup.ConditionExpression;

                if (string.IsNullOrEmpty(conditionExpression))
                    continue;

                string configuration, plattform;

                if (!ParseCondition(conditionExpression, out configuration, out plattform))
                    continue;

                configurationNames.Add(configuration);
                platformNames.Add(plattform);
            }
        }

        internal static bool ParseCondition([NotNull] this string conditionExpression, out string configuration, out string platform)
        {
            Contract.Requires(conditionExpression != null);

            configuration = platform = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(conditionExpression))
                    return false;

                var parts = conditionExpression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length != 2)
                    return false;

                var expression = parts[0].Trim().Replace("$(", "(?<").Replace(")", ">.+?)").Replace("|", "\\|");

                // Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'"
                // Regex: '(^<Configuration>.+?)\|(^Platform>.+?)'
                var regex = new Regex(expression);
                var match = regex.Match(parts[1].Trim());

                configuration = match.Groups["Configuration"]?.Value;
                platform = (match.Groups["Platform"]?.Value ?? "AnyCPU").Replace("AnyCPU", "Any CPU");

                return !string.IsNullOrEmpty(configuration) && !string.IsNullOrEmpty(platform);
            }
            catch
            {
                // some unknown expression..
                return false;
            }
        }

        internal static Guid GetProjectGuid([NotNull] this IServiceProvider serviceProvider, [NotNull] IVsHierarchy projectHierarchy)
        {
            Contract.Requires(serviceProvider != null);
            Contract.Requires(projectHierarchy != null);

            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Contract.Assume(solution != null);

            Guid projectGuid;
            solution.GetGuidOfProject(projectHierarchy, out projectGuid);
            return projectGuid;
        }
    }
}