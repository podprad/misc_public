namespace SwallowTest
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using MyClassLibrary;

    public static class MyResolver
    {
        public static IEnumerable<Type> GetAllKnownTypes(ICustomAttributeProvider provider)
        {
            var result = new List<Type>();

            result.Add(typeof(MyRequest));
            result.Add(typeof(MyResponse));

            return result;
        }
    }
}