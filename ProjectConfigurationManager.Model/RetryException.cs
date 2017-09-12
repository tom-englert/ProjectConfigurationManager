namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    [Serializable]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public class RetryException : Exception
    {
        [UsedImplicitly]
        public RetryException()
        {
        }

        public RetryException(string message)
            : base(message)
        {
        }

        public RetryException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public RetryException(Exception inner)
            : base(string.Empty, inner)
        {
        }

        protected RetryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
