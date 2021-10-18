using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using SerializerScript.Debug;

namespace SerializerScript.Internal
{
    public enum PrefixStyle
    {
        None,
        Base128,
        Fixed32,
        Fixed32BigEndian
    }
    public enum WireType
    {
        None = -1,
        Variant = 0,
        Fixed64 = 1,
        String = 2,
        StartGroup = 3,
        EndGroup = 4,
        Fixed32 = 5,
        SignedVariant = 8
    }
    public struct SubItemToken
    {
        internal readonly int value;

        internal SubItemToken(int value)
        {
            this.value = value;
        }
    }
    internal sealed class Helpers
    {
        public static readonly Type[] EmptyTypes = Type.EmptyTypes;


        private Helpers()
        {
        }

        public static void DebugAssert(bool state,string err)
        {
            DebugHelper.DebugAssert(state, err);
        }
        public static void DebugAssert(bool state)
        {
            DebugHelper.DebugAssert(state, "");
        }
        public static StringBuilder AppendLine(StringBuilder builder)
        {
            return builder.AppendLine();
        }

        public static bool IsNullOrEmpty(string value)
        {
            return value == null || value.Length == 0;
        }

        public static void BlockCopy(byte[] from, int fromIndex, byte[] to, int toIndex, int count)
        {
            Buffer.BlockCopy(from, fromIndex, to, toIndex, count);
        }

        public static bool IsInfinity(float value)
        {
            return float.IsInfinity(value);
        }

        internal static MethodInfo GetInstanceMethod(Type declaringType, string name)
        {
            return declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        internal static MethodInfo GetStaticMethod(Type declaringType, string name)
        {
            return declaringType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        internal static MethodInfo GetInstanceMethod(Type declaringType, string name, Type[] types)
        {
            if (types == null)
            {
                types = EmptyTypes;
            }
            return declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        }

        internal static bool IsSubclassOf(Type type, Type baseClass)
        {
            return type.IsSubclassOf(baseClass);
        }

        public static bool IsInfinity(double value)
        {
            return double.IsInfinity(value);
        }

        internal static Type GetUnderlyingType(Type type)
        {
            return Nullable.GetUnderlyingType(type);
        }

        internal static bool IsValueType(Type type)
        {
            return type.IsValueType;
        }

        internal static bool IsEnum(Type type)
        {
            return type.IsEnum;
        }

        internal static MethodInfo GetGetMethod(PropertyInfo property, bool nonPublic, bool allowInternal)
        {
            if (property == null)
            {
                return null;
            }
            MethodInfo methodInfo = property.GetGetMethod(nonPublic);
            if (methodInfo == null && !nonPublic && allowInternal)
            {
                methodInfo = property.GetGetMethod(nonPublic: true);
                if (methodInfo == null && !methodInfo.IsAssembly && !methodInfo.IsFamilyOrAssembly)
                {
                    methodInfo = null;
                }
            }
            return methodInfo;
        }

        internal static MethodInfo GetSetMethod(PropertyInfo property, bool nonPublic, bool allowInternal)
        {
            if (property == null)
            {
                return null;
            }
            MethodInfo methodInfo = property.GetSetMethod(nonPublic);
            if (methodInfo == null && !nonPublic && allowInternal)
            {
                methodInfo = property.GetGetMethod(nonPublic: true);
                if (methodInfo == null && !methodInfo.IsAssembly && !methodInfo.IsFamilyOrAssembly)
                {
                    methodInfo = null;
                }
            }
            return methodInfo;
        }

        internal static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes, bool nonPublic)
        {
            return type.GetConstructor(nonPublic ? (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : (BindingFlags.Instance | BindingFlags.Public), null, parameterTypes, null);
        }

        internal static ConstructorInfo[] GetConstructors(Type type, bool nonPublic)
        {
            return type.GetConstructors(nonPublic ? (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : (BindingFlags.Instance | BindingFlags.Public));
        }

        internal static PropertyInfo GetProperty(Type type, string name, bool nonPublic)
        {
            return type.GetProperty(name, nonPublic ? (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : (BindingFlags.Instance | BindingFlags.Public));
        }

        internal static object ParseEnum(Type type, string value)
        {
            return Enum.Parse(type, value, ignoreCase: true);
        }

        internal static MemberInfo[] GetInstanceFieldsAndProperties(Type type, bool publicOnly)
        {
            BindingFlags bindingAttr = (publicOnly ? (BindingFlags.Instance | BindingFlags.Public) : (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            PropertyInfo[] properties = type.GetProperties(bindingAttr);
            FieldInfo[] fields = type.GetFields(bindingAttr);
            MemberInfo[] array = new MemberInfo[fields.Length + properties.Length];
            properties.CopyTo(array, 0);
            fields.CopyTo(array, properties.Length);
            return array;
        }

        internal static Type GetMemberType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
            };
            return null;
        }

        internal static bool IsAssignableFrom(Type target, Type type)
        {
            return target.IsAssignableFrom(type);
        }
    }
    internal sealed class BufferPool
    {
        private const int PoolSize = 20;

        internal const int BufferLength = 1024;

        private static readonly object[] pool = new object[20];

        internal static void Flush()
        {
            lock (pool)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    pool[i] = null;
                }
            }
        }

        private BufferPool()
        {
        }

        internal static byte[] GetBuffer()
        {
            lock (pool)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    object obj;
                    if ((obj = pool[i]) != null)
                    {
                        pool[i] = null;
                        return (byte[])obj;
                    }
                }
            }
            return new byte[1024];
        }

        internal static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
        {
            int num = buffer.Length * 2;
            if (num < toFitAtLeastBytes)
            {
                num = toFitAtLeastBytes;
            }
            byte[] array = new byte[num];
            if (copyBytes > 0)
            {
                Helpers.BlockCopy(buffer, copyFromIndex, array, 0, copyBytes);
            }
            if (buffer.Length == 1024)
            {
                ReleaseBufferToPool(ref buffer);
            }
            buffer = array;
        }

        internal static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }
            if (buffer.Length == 1024)
            {
                lock (pool)
                {
                    for (int i = 0; i < pool.Length; i++)
                    {
                        if (pool[i] == null)
                        {
                            pool[i] = buffer;
                            break;
                        }
                    }
                }
            }
            buffer = null;
        }
    }

}
