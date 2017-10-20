namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using JetBrains.Annotations;

    [Serializable]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public sealed class RetryException : Exception
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
