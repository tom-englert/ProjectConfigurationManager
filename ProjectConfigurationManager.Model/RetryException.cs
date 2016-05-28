namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;

    internal class RetryException : Exception
    {
        public RetryException(Exception inner)
            : base(string.Empty, inner)
        {
        }
    }

}
