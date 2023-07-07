namespace SwallowTest
{
    using System;
    using System.Collections.ObjectModel;
    using CoreWCF;
    using CoreWCF.Channels;
    using CoreWCF.Description;
    using CoreWCF.Dispatcher;

    public class ErrorBehavior : IServiceBehavior
    {
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            IErrorHandler errorHandler;

            try
            {
                errorHandler = new MyErrorHandler();
            }
            catch (MissingMethodException e)
            {
                throw new ArgumentException("The errorHandlerType specified in the ErrorBehaviorAttribute constructor must have a public empty constructor.", e);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException("The errorHandlerType specified in the ErrorBehaviorAttribute constructor must implement System.ServiceModel.Dispatcher.IErrorHandler.", e);
            }

            foreach (ChannelDispatcherBase channelDispatcherBase in serviceHostBase.ChannelDispatchers)
            {
                ChannelDispatcher channelDispatcher = channelDispatcherBase as ChannelDispatcher;
                channelDispatcher.ErrorHandlers.Add(errorHandler);

                channelDispatcher.IncludeExceptionDetailInFaults = true;

                channelDispatcher.Faulted += (sender, args) =>
                {
                };
            }
        }
    }
}