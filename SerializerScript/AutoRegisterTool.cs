using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerializerScript
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AutoGeneraterSerializerCodeAttribute : Attribute
    {
        public AutoGeneraterSerializerCodeAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializerNumberAttribute : Attribute
    {
        public int number;
        public SerializerNumberAttribute(int num) { number = num; }
    }

    namespace AutoGenaterCode
    {
        public static partial class AutoRegisterTool
        {
            public static void DoEmtpty() { }
        }
    }

}
