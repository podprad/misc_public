namespace SameBaseUrl
{
    using System.Threading.Tasks;

    public class MyService : IMyService
    {
        public Task<MyResponse> ExecuteDynamic()
        {
            return Task.FromResult(new MyResponse
            {
                Output = "Hello",
            });
        }
    }
}