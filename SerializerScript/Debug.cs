using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerializerScript.Debug
{
    public static class DebugHelper
    {
        public static void LogError(string error)
        {
            UnityEngine.Debug.LogError(error);
        }

        public static void DebugAssert(bool state, string err)
        {
            UnityEngine.Debug.Assert(state, err);
        }
    }

}
