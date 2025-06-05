using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace FreePIE.Core.Common.Extensions
{
    public static class TypeExtensions
    {
        public static Type[] GetTypes(this Type type)
        {
            Type[] types = new Type[] { };
            try
            {
                types = type.Assembly.GetTypes();
            }
            catch(ReflectionTypeLoadException lx)
            {
                Debug.WriteLine(string.Join(Environment.NewLine, lx.LoaderExceptions.Select(l => l.Message)));
            }


            var retval = types.Where(t => !t.IsAbstract && (type.IsGenericType ? t.IsAssignableFrom(type.GetGenericTypeDefinition()) : type.IsAssignableFrom(t)))
                .ToArray();

            return retval;
        }

        public static Type[] GetTypesSafe(this Assembly assembly)
        {
            Type[] types = new Type[] { };
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException lx)
            {
                Debug.WriteLine(string.Join(Environment.NewLine, lx.LoaderExceptions.Select(l => l.Message)));
            }

            return types;
        }
    }
}
