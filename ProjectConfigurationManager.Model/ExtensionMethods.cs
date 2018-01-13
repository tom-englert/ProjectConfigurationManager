namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio.Shell.Interop;

    internal static class ExtensionMethods
    {
        public static void RemoveSelfAndWhitespace([NotNull] this XElement element)
        {
            while (true)
            {
                if ((element.PreviousNode is XText previous) && string.IsNullOrWhiteSpace(previous.Value))
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

        public static bool MatchesConfiguration([NotNull] this IPropertyGroup propertyGroup, [CanBeNull] string configuration, [CanBeNull] string platform)
        {
            var conditionExpression = propertyGroup.ConditionExpression;

            if (string.IsNullOrEmpty(conditionExpression))
                return string.IsNullOrEmpty(configuration) && string.IsNullOrEmpty(platform);

            return conditionExpression.ParseCondition(out var groupConfiguration, out string groupPlatform)
                   && configuration == groupConfiguration
                   && platform == groupPlatform;
        }

        [NotNull, ItemNotNull]
        internal static IEnumerable<ProjectConfiguration> GetProjectConfigurations([NotNull] this Project project)
        {
            var configurationNames = new HashSet<string> { "Debug", "Release" };
            var platformNames = new HashSet<string>();

            var projectFile = project.ProjectFile;

            ParseConfigurations(projectFile, configurationNames, platformNames);

            if (!platformNames.Any())
                platformNames.Add("Any CPU");

            var projectConfigurations = configurationNames
                .SelectMany(configuration => platformNames.Select(platform => new ProjectConfiguration(project, configuration, platform)));

            return projectConfigurations;
        }

        private static void ParseConfigurations([NotNull] this ProjectFile projectFile, [NotNull, ItemNotNull] ICollection<string> configurationNames, [NotNull, ItemNotNull] ICollection<string> platformNames)
        {
            foreach (var propertyGroup in projectFile.PropertyGroups)
            {
                var conditionExpression = propertyGroup.ConditionExpression;

                if (string.IsNullOrEmpty(conditionExpression))
                    continue;

                if (!ParseCondition(conditionExpression, out var configuration, out var plattform))
                    continue;

                configurationNames.Add(configuration);
                platformNames.Add(plattform);
            }
        }

        private static bool ParseCondition([NotNull] this string conditionExpression, [CanBeNull] out string configuration, [NotNull] out string platform)
        {
            configuration = platform = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(conditionExpression))
                    return false;

                var parts = conditionExpression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length != 2)
                    return false;

                // ReSharper disable once PossibleNullReferenceException
                var expression = parts[0].Trim().Replace("$(", "(?<").Replace(")", ">.+?)").Replace("|", "\\|");

                // Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'"
                // Regex: '(^<Configuration>.+?)\|(^Platform>.+?)'
                var regex = new Regex(expression);
                // ReSharper disable once PossibleNullReferenceException
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

        public static Guid GetProjectGuid([NotNull] this IServiceProvider serviceProvider, [NotNull] IVsHierarchy projectHierarchy)
        {
            var solution = (IVsSolution) serviceProvider.GetService(typeof(SVsSolution));

            Debug.Assert(solution != null, nameof(solution) + " != null");
            solution.GetGuidOfProject(projectHierarchy, out var projectGuid);
            return projectGuid;
        }

        [NotNull]
        public static XElement AddElement([NotNull] this XElement parent, [NotNull] XElement child)
        {
            var lastNode = parent.LastNode;

            if (lastNode?.NodeType == XmlNodeType.Text)
            {
                var lastDelimiter = lastNode.PreviousNode?.PreviousNode as XText;
                var whiteSpace = new XText(lastDelimiter?.Value ?? "\n    ");
                lastNode.AddBeforeSelf(whiteSpace, child);
            }
            else
            {
                parent.Add(child);
            }

            return child;
        }

    }
}