namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;

    using Equatable;

    using JetBrains.Annotations;

    [ImplementsEquatable]
    public sealed class ProjectPropertyName
    {
        internal ProjectPropertyName([NotNull] IProjectProperty property)
        {
            Name = property.Name;
            var label = property.Group.Label;
            GroupName = !string.IsNullOrEmpty(label) ? new PropertyGroupName(label) : PropertyGroupName.GetGroupForProperty(Name);
            DisplayName = GetDisplayName(Name, GroupName.Name);
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
