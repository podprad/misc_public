namespace MyClassLibrary
{
    using System.Runtime.Serialization;

    [DataContract]
    public class MyRequest
    {
        [DataMember]
        public string Input { get; }
    }
}