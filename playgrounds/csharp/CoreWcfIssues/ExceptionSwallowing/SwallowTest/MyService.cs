namespace SwallowTest
{
    using System.Threading.Tasks;
    using MyClassLibrary;

    public class MyService : IMyService
    {
        public Task<MyResponse> ExecuteDynamic(MyRequest request)
        {
            return Task.FromResult(new MyResponse
            {
            });
        }
    }
}