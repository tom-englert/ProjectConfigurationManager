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

        [NotNull, Equals(StringComparison.OrdinalIgnoreCase)]
        public string Name { get; }

        [NotNull]
        public PropertyGroupName GroupName { get; }

        [NotNull]
        public string DisplayName { get; }

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
