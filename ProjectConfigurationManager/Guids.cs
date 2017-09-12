// Guids.cs
// MUST match guids.h

namespace tomenglertde.ProjectConfigurationManager
{
    using System;

    internal static class GuidList
    {
        public const string guidProjectConfigurationManagerPkgString = "e31595c9-3e0c-4f5c-b35c-dd8d61e364d1";
        public const string guidProjectConfigurationManagerCmdSetString = "21c19afd-a1ab-47f2-8df3-daa27df56e46";
        public const string guidToolWindowPersistanceString = "01a9a1a2-ea6f-4cb6-ae33-996b06435a62";

        public static readonly Guid guidProjectConfigurationManagerCmdSet = new Guid(guidProjectConfigurationManagerCmdSetString);
    };
}