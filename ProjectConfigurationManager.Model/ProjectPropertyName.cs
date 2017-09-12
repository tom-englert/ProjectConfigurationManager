namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [Equals]
    public sealed class ProjectPropertyName
    {
        internal ProjectPropertyName([NotNull] string name, [NotNull] PropertyGroupName groupName)
        {
            Contract.Requires(name != null);
            Contract.Requires(groupName != null);

            Name = name;
            GroupName = groupName;
            DisplayName = HasGroupNamePrefix(name, groupName.Name) ? name.Substring(groupName.Name.Length) : name;
        }

        private static bool HasGroupNamePrefix([NotNull] string name, [NotNull] string groupName)
        {
            Contract.Requires(name != null);
            Contract.Requires(groupName != null);
            Contract.Ensures((Contract.Result<bool>() == false) || (name.Length > groupName.Length));

            var length = groupName.Length;

            return name.Length > length && name.StartsWith(groupName, StringComparison.Ordinal) && char.IsUpper(name[length]);
        }

        [NotNull, IgnoreDuringEquals]
        public string Name { get; }

        [NotNull, IgnoreDuringEquals]
        public PropertyGroupName GroupName { get; }

        [NotNull, IgnoreDuringEquals]
        public string DisplayName { get; }

        [CustomGetHashCode, UsedImplicitly]
        private int CustomGetHashCode()
        {
            return Name.ToUpperInvariant().GetHashCode();
        }

        [CustomEqualsInternal, UsedImplicitly]
        private bool CustomEquals([NotNull] ProjectPropertyName other)
        {
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Name != null);
            Contract.Invariant(GroupName != null);
            Contract.Invariant(DisplayName != null);
        }
    }
}
