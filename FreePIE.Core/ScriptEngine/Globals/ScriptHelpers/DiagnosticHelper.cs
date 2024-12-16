using System;
using System.Linq;

using FreePIE.Core.Common.Events;
using FreePIE.Core.Common.Extensions;
using FreePIE.Core.Contracts;
using FreePIE.Core.Model.Events;

namespace FreePIE.Core.ScriptEngine.Globals.ScriptHelpers
{
    [Global(Name = "diagnostics")]
    public class DiagnosticHelper : IScriptHelper
    {
        public static string Version = "unknown";

        private readonly IEventAggregator eventAggregator;

        public DiagnosticHelper(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public void debug(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void debug(object arg)
        {
            Console.WriteLine(arg);
        }

        [NeedIndexer]
        public void watch(object value, string indexer)
        {
            
            if(value != null && !IsSimple(value.GetType()))
            {
                watchObject(value, indexer);
                return;
            }
            eventAggregator.Publish(new WatchEvent(indexer, value));
        }

        [NeedIndexer]
        public void watchObject(object obj, params string[] properties)
        {
            if (obj == null)
            {
                return;
            }

            var args = properties.Take(properties.Length - 1).ToArray();
            var indexer = properties[properties.Length - 1].Split(',')[0];

            if (args.Length == 0)
            {
                watchExcept(obj, indexer);
            }
            else
            {
                foreach (var pair in obj.EnumerateRuntimeProperties(false, args))
                {
                    watch(pair.Value, indexer + "." + pair.Key);
                }
            }
        }

        bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        [NeedIndexer]
        public void watchExcept(object obj,params string[] properties)
        {
            if (obj == null)
            {
                return;
            }

            var args = properties.Take(properties.Length - 1).ToArray();
            var indexer = properties[properties.Length - 1];

            foreach (var pair in obj.EnumerateRuntimeProperties(true, args ))
            {
                watch(pair.Value, indexer + "." + pair.Key);
            }
        }

        public void notify(string message)
        {
            eventAggregator.Publish(new TrayNotificationEvent(message, ""));

        }
        public void notify(string title, string message)
        {
            eventAggregator.Publish(new TrayNotificationEvent(message, title));

        }
        public void notify(string title,string message, params object[] args)
        {
            eventAggregator.Publish(new TrayNotificationEvent(string.Format(message, args),title));
           
        }

        public string version()
        {
            return Version;
        }
    }
}
