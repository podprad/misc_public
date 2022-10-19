using System;

namespace MyLibrary
{
    public static class ClassWithMain
    {
        public static int DotnetMain(IntPtr arg, int argLength)
        {
            Helpers.LogEnter();

            Helpers.LogAppDomainInformation();

            Helpers.LogAssemblyLoadContextInformation();

            Helpers.LogLeave();
            return 0;
        }           
    }
}