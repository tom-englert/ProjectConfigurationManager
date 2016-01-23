namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    public class ProjectPropertyName : IEquatable<ProjectPropertyName>
    {
        private readonly string _name;
        private readonly string _groupName;
        private readonly string _displayName;

        internal ProjectPropertyName(string name, string groupName)
        {
            Contract.Requires(name != null);
            Contract.Requires(groupName != null);

            _name = name;
            _groupName = groupName;
            _displayName = HasGroupNamePrefix(name, groupName) ? name.Substring(groupName.Length) : name;
        }

        private static bool HasGroupNamePrefix(string name, string groupName)
        {
            Contract.Requires(name != null);
            Contract.Requires(groupName != null);
            Contract.Ensures((Contract.Result<bool>() == false) || (name.Length > groupName.Length));

            var length = groupName.Length;

            return name.Length > length && name.StartsWith(groupName) && char.IsUpper(name[length]);
        }

        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _name;
            }
        }

        public string GroupName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _groupName;
            }
        }

        public string DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _displayName;
            }
        }

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
            return Equals(obj as ProjectPropertyName);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ProjectPropertyName"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ProjectPropertyName"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="ProjectPropertyName"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ProjectPropertyName other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ProjectPropertyName left, ProjectPropertyName right)
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
        public static bool operator ==(ProjectPropertyName left, ProjectPropertyName right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ProjectPropertyName left, ProjectPropertyName right)
        {
            return !InternalEquals(left, right);
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_name != null);
            Contract.Invariant(_groupName != null);
            Contract.Invariant(_displayName != null);
        }
    }
}
