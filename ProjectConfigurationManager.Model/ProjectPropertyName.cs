namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using Equatable;

    using JetBrains.Annotations;

    [ImplementsEquatable]
    public sealed class ProjectPropertyName
    {
        internal ProjectPropertyName([NotNull] string name, [NotNull] PropertyGroupName groupName)
        {
            Name = name;
            GroupName = groupName;
            DisplayName = GetDisplayName(name, groupName.Name);
        }

        [NotNull]
        private static string GetDisplayName([NotNull] string name, [NotNull] string groupName)
        {
            var groupNameLength = groupName.Length;

            if (name.Length > groupNameLength && name.StartsWith(groupName, StringComparison.Ordinal))
            {
                if (name[groupNameLength] == '.')
                    return name.Substring(groupNameLength + 1);

                if (char.IsUpper(name[groupNameLength]))
                    return name.Substring(groupNameLength);
            }

            return name;
        }

        [NotNull, Equals(StringComparison.OrdinalIgnoreCase)]
        public string Name { get; }

        [NotNull]
        public PropertyGroupName GroupName { get; }

        [NotNull]
        public string DisplayName { get; }
    }
}
