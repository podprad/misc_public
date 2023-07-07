namespace MyClassLibrary
{
    using System.Runtime.Serialization;

    [DataContract]
    public class MyResponse
    {
        [DataMember]
        public string Output { get; }
    }
}