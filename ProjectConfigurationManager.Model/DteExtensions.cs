namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;

    public static class DteExtensions
    {
        public static IEnumerable<EnvDTE.Property> GetProperties(this EnvDTE.Properties properites)
        {
            for (var i = 1; i <= properites.Count; i++)
            {
                EnvDTE.Property property;
                try
                {
                    property = properites.Item(i);
                }
                catch
                {
                    // trace.TraceError("Error loading property #" + i);
                    continue;
                }

                yield return property;
            }
        }
    }
}
