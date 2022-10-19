using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;

namespace MyLibrary
{
    internal class ClassWithDllExport
    {
        [DllExport("HelloDllExport", CallingConvention = CallingConvention.StdCall)]
        public static void HelloDllExport()
        {
            Helpers.LogEnter();

            Helpers.LogAppDomainInformation();

            Helpers.LogAssemblyLoadContextInformation();

            UseNewtonsoft();

            Helpers.LogAssemblyLoadContextInformation();            

            Helpers.LogLeave();
        }

        private static void UseNewtonsoft()
        {
            try
            {
                var result = JsonConvert.SerializeObject(new { TestProp = "Hello" });
                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }
}
