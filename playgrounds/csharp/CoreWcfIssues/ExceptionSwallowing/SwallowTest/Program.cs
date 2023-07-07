namespace SwallowTest
{
    using System;
    using System.Runtime.ExceptionServices;
    using CoreWCF;
    using CoreWCF.Configuration;
    using CoreWCF.Description;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddTransient(typeof(MyService), s => new MyService());

            builder.Services.AddServiceModelServices()
                .AddServiceModelMetadata()
                .AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

            // does not work
            builder.Services.AddSingleton<IServiceBehavior, ErrorBehavior>(s => new ErrorBehavior());

            var app = builder.Build();

            app.UseServiceModel(b =>
            {
                b.AddService(typeof(MyService), serviceOptions => { serviceOptions.BaseAddresses.Add(new Uri("http://localhost:63144")); });

                b.ConfigureServiceHostBase(typeof(MyService), serviceHostBase =>
                {
                    var serviceMetadataBehavior = new ServiceMetadataBehavior
                    {
                        HttpGetEnabled = true,
                    };

                    serviceHostBase.Description.Behaviors.Remove<ServiceMetadataBehavior>();
                    serviceHostBase.Description.Behaviors.Add(serviceMetadataBehavior);

                    serviceHostBase.Faulted += (sender, eventArgs) =>
                    {
                        // does not work
                    };
                });

                var binding = new WSHttpBinding(SecurityMode.None);

                b.AddServiceEndpoint(typeof(MyService), typeof(IMyService), binding, new Uri("http://localhost:63144/MyService.svc"), null, serviceEndpoint => { });

                b.Faulted += (sender, eventArgs) =>
                {
                    // This one is called, however we don't get any Exception details.
                };
            });

            app.Run();
        }
    }
}