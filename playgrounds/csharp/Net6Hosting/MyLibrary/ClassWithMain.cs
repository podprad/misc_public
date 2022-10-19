using System;
using System.Runtime.InteropServices;

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

        [return: MarshalAs(UnmanagedType.I4)]
        public static int DotnetMainVoid()
        {
            Helpers.LogEnter();

            Helpers.LogAppDomainInformation();

            Helpers.LogAssemblyLoadContextInformation();

            Helpers.LogLeave();

            return 0;
        }
    }
}