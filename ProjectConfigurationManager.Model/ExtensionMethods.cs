namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    public static class ExtensionMethods
    {
        public static void RemoveSelfAndWhitespace(this XElement element)
        {
            Contract.Requires(element != null);

            var previous = element.PreviousNode as XText;
            if ((previous != null) && string.IsNullOrWhiteSpace(previous.Value))
            {
                previous.Remove();
            }

            element.Remove();
        }
    }
}