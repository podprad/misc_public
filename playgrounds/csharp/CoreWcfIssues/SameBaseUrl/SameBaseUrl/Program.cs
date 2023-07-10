namespace SameBaseUrl
{
    using System;
    using System.Diagnostics;
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
                .AddServiceModelWebServices()
                .AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

            var app = builder.Build();

            app.UseServiceModel(b =>
            {
                b.AddService(typeof(MyService), serviceOptions =>
                {
                    serviceOptions.BaseAddresses.Add(new Uri("http://localhost:58232/MyService.svc"));
                });

                b.ConfigureServiceHostBase(typeof(MyService), serviceHostBase =>
                {
                    var serviceMetadataBehavior = new ServiceMetadataBehavior
                    {
                        HttpGetEnabled = true,
                    };

                    serviceHostBase.Description.Behaviors.Remove<ServiceMetadataBehavior>();
                    serviceHostBase.Description.Behaviors.Add(serviceMetadataBehavior);

                    serviceHostBase.Faulted += OnUnexpectedError;
                });

                var binding = new WSHttpBinding(SecurityMode.None);

                // This works fine.
                // b.AddServiceEndpoint<MyService, IMyService>(binding, "/wsHttp");

                // This makes /json does not work.
                b.AddServiceEndpoint<MyService, IMyService>(binding, "/");

                // curl -i -X POST http://localhost:58232/MyService.svc/json/ExecuteDynamic -d "{}" --insecure
                var webBinding = new WebHttpBinding(WebHttpSecurityMode.None);
                b.AddServiceWebEndpoint<MyService, IMyService>(webBinding, "/json");

                b.Faulted += OnUnexpectedError;
            });

            app.Run();
        }

        private static void OnUnexpectedError(object sender, EventArgs args)
        {
            Debug.WriteLine("Error");
        }
    }
}