namespace SwallowTest
{
    using System;
    using CoreWCF.Channels;
    using CoreWCF.Dispatcher;

    public class MyErrorHandler : IErrorHandler
    {
        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            // not called
        }

        public bool HandleError(Exception error)
        {
            // not called
            return true;
        }
    }
}