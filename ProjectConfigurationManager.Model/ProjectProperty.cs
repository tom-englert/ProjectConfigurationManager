namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    public class ProjectProperty : IEquatable<ProjectProperty>
    {
        private readonly string _name;
        private readonly string _groupName;
        private readonly string _displayName;

        internal ProjectProperty(string name, string groupName)
        {
            Contract.Requires(name != null);

            _name = name;
            _groupName = groupName;
            _displayName = name.StartsWith(groupName) ? name.Substring(groupName.Length) : name;
        }

        public string Name => _name;

        public string GroupName => _groupName;

        public string DisplayName => _displayName;

        #region IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _name.ToUpperInvariant().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ProjectProperty);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProjectProperty"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ProjectProperty"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProjectProperty"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProjectProperty other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ProjectProperty left, ProjectProperty right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return string.Equals(left._name, right._name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ProjectProperty left, ProjectProperty right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ProjectProperty left, ProjectProperty right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_name != null);
        }
    }
}
