namespace SwallowTest
{
    using System.Threading.Tasks;
    using CoreWCF;
    using MyClassLibrary;

    [ServiceContract]
    [ServiceKnownType(nameof(MyResolver.GetAllKnownTypes), typeof(MyResolver))]
    public interface IMyService
    {
        [OperationContract]
        Task<MyResponse> ExecuteDynamic(MyRequest request);
    }
}