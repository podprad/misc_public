namespace SameBaseUrl
{
    using System.Threading.Tasks;
    using CoreWCF;
    using CoreWCF.Web;

    [ServiceContract]
    public interface IMyService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<MyResponse> ExecuteDynamic();
    }
}