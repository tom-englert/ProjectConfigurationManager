namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System.Runtime.Serialization;

    [DataContract]
    public class PropertyGrouping
    {
        [DataMember]
        public PropertyGroup[] Groups { get; set; }
    }

    [DataContract]
    public class PropertyGroup
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string[] PropertyPatterns { get; set; }
    }
}
