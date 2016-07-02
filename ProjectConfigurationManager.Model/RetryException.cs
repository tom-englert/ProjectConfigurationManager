namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class RetryException : Exception
    {
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
