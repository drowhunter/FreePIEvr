using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreePIE.Core.Common.Extensions
{
    public static class DiagnosticExtensions
    {
        /// <summary>
        /// Enumerate the runtime properties of an object
        /// </summary>
        /// <param name="self"></param>
        /// <param name="except"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        internal static IEnumerable<KeyValuePair<string, object>> EnumerateRuntimeProperties(this object self, bool except, params object[] properties)
        {
            var t = self.GetType();


            foreach (var pair in self.EnumerateRuntimeProperties())
            {
                bool match = false;
                try
                {
                    match =
                       properties.Any(
                           p =>
                               string.Equals(p.ToString(), pair.Key, StringComparison.InvariantCultureIgnoreCase));

                    if (except)
                        match = !match;
                }
                catch (Exception x)
                {

                }

                if (match)
                {
                    // check if pair Value is an object

                    var val = pair.Value;

                    if(IsClass(val))
                    {
                        foreach (var pair1 in val.EnumerateRuntimeProperties())
                        {
                            yield return new KeyValuePair<string, object>($"{pair.Key}.{pair1.Key}", pair1.Value);
                        }
                    }
                    else
                        yield return pair;
                }

            }

        }

        private static bool IsClass(object val)
        {
            return val != null && val.GetType().IsClass && val.GetType() != typeof(string);
        }

        internal static IEnumerable<KeyValuePair<string, object>> EnumerateRuntimeProperties(this object self)
        {
            var t = self.GetType();
            foreach (var runtimeProperty in t.GetRuntimeProperties())
            {
                //if (runtimeProperty.GetIndexParameters().Length > 0)
                //{
                //    continue;

                //}
                object v = null;
                try
                {
                    var idxparams = runtimeProperty.GetIndexParameters();
                    if (idxparams.Length == 0)
                        v = runtimeProperty.GetValue(self);
                    else
                    {
                        string idx = "";
                        foreach (var parameterInfo in idxparams)
                        {
                            //if(parameterInfo.HasDefaultValue)

                        }
                    }
                }
                catch (Exception x)
                {

                }

                if (v is IEnumerable enumerable && enumerable.GetType() != typeof(string))
                {
                    string vss = Environment.NewLine;

                    if (enumerable.GetType().Name.Contains("Boolean"))
                    {
                        var o = enumerable.Cast<bool>().ToArray();
                        List<string> h = new List<string>(o.Length);

                        for (int i = 0; i < o.Length; i++)
                        {
                            var istring = i.ToString();
                            if (istring.Length == 1)
                            {
                                istring = istring.PadLeft(2, '0');
                            }

                            vss += $"[{(o[i] ? $"({istring})" : "  ")}]{((i % 10 == 0) ? Environment.NewLine : "")}";
                        }
                    }
                    else
                    {
                        vss = enumerable.Cast<object>().Aggregate("", (current, o) => Convert.ToString(o) + Environment.NewLine);
                    }

                    yield return new KeyValuePair<string, object>(runtimeProperty.Name, vss);
                }
                else
                    yield return new KeyValuePair<string, object>(runtimeProperty.Name, v ?? "NULL");

            }
        }
    }
}
