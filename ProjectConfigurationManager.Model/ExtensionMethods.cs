namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;

    internal static class ExtensionMethods
    {
        public static void RemoveSelfAndWhitespace(this XElement element)
        {
            Contract.Requires(element != null);

            while (true)
            {
                var previous = element.PreviousNode as XText;

                if ((previous != null) && string.IsNullOrWhiteSpace(previous.Value))
                {
                    previous.Remove();
                }

                var parent = element.Parent;

                element.Remove();

                if ((parent == null) || parent.HasElements)
                    return;

                element = parent;
            }
        }
    }
}