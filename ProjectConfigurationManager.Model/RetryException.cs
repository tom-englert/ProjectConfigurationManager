namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using JetBrains.Annotations;

    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    internal sealed class RetryException : Exception
    {
        [UsedImplicitly]
        public RetryException()
        {
        }

        public RetryException([NotNull] string message)
            : base(message)
        {
        }

        public RetryException([NotNull] string message, [NotNull] Exception inner)
            : base(message, inner)
        {
        }

        public RetryException([NotNull] Exception inner)
            : base(string.Empty, inner)
        {
        }
    }
}
