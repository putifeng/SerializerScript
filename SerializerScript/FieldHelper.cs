using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SerializerScript
{
    public static class FieldHelper
    {
        public static T GetCustomAttribute<T>(this FieldInfo fieldInfo, bool inherit = false)
        {
            var rets = fieldInfo.GetCustomAttributes(typeof(T), inherit);
            if (rets == null)
                return default(T);
            if (rets.Length == 0)
                return default(T);
            return (T)rets[0];
        }

        public static T GetCustomAttribute<T>(this Type type, bool inherit = false)
        {
            var rets = type.GetCustomAttributes(typeof(T), inherit);
            if (rets == null)
                return default(T);
            if (rets.Length == 0)
                return default(T);
            return (T)rets[0];
        }
    }
}
