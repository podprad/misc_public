using System.Runtime.CompilerServices;
using System;
using System.Runtime.Loader;

namespace MyLibrary
{
    internal static class Helpers
    {
        public static void LogEnter([CallerMemberName] string callerMemberName = null)
        {
            Console.WriteLine($"===> {callerMemberName}");
        }

        public static void LogLeave([CallerMemberName] string callerMemberName = null)
        {
            Console.WriteLine($"<=== {callerMemberName}");
        }

        public static void LogAppDomainInformation()
        {
            Console.WriteLine("### AppDomain info");
            Console.WriteLine("BaseDirectory=" + AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine("Friendly name=" + AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("RelativeSearchPath=" + AppDomain.CurrentDomain.RelativeSearchPath);
            Console.WriteLine("TFN=" + AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
            Console.WriteLine("ApplicationBase=" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
            Console.WriteLine();
        }

        public static void LogAssemblyLoadContextInformation()
        {
            var contexts = AssemblyLoadContext.All;

            foreach (var context in contexts)
            {
                Console.WriteLine("### AssemblyLoadContext name = " + context.Name);
                
                foreach(var assembly in context.Assemblies)
                {
                    Console.WriteLine("  " + assembly.FullName);
                }

                Console.WriteLine();
            }
        }
    }
}
