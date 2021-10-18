using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SerializerScript.AutoGenaterCode;
using SerializerScript.Internal;

namespace SerializerScript
{
    public class CostomSerilzaerModel
    {
        public static CostomSerilzaerModel Instance = new CostomSerilzaerModel();

        private static Dictionary<Type, ISerializerObject> toolMaps = new Dictionary<Type, ISerializerObject>();

        static CostomSerilzaerModel()
        {
            AutoRegisterTool.DoEmtpty();
        }

        public static void RegisterISerializerObject(Type type, ISerializerObject serializer)
        {
            toolMaps[type] = serializer;
        }

        public static T Read<T>(ReaderHelper reader)
        {
            ISerializerObject toolbase = null;
            toolMaps.TryGetValue(typeof(T), out toolbase);
            ISerializerObject<T> tools = toolbase as ISerializerObject<T>;
            return tools.Read(reader);
        }

        public static void Write<T>(T obj, WriterHelper writer)
        {
            ISerializerObject toolbase = null;
            toolMaps.TryGetValue(obj.GetType(), out toolbase);

            ISerializerObject<T> tools = toolbase as ISerializerObject<T>;
            if(tools == null)
            {
               Debug.DebugHelper.LogError("tools is null." + obj.GetType().Name);
            }
            tools.Write(obj, writer);
        }

        public static T ReadObjectInternal<T>(ReaderHelper reader)
        {
            ISerializerObject toolbase = null;
            toolMaps.TryGetValue(typeof(T), out toolbase);

            ISerializerObject<T> tools = toolbase as ISerializerObject<T>;

            SubItemToken token = ReaderHelper.StartSubItem(reader);
            T ret = DeserializeByType<T>(tools, reader);
            ReaderHelper.EndSubItem(token, reader);
            return ret;
        }

        public static void WriteObjectInternal<T>(T obj, WriterHelper writer)
        {
            ISerializerObject toolbase = null;
            toolMaps.TryGetValue(obj.GetType(), out toolbase);

            ISerializerObject<T> tools = toolbase as ISerializerObject<T>;
            SubItemToken token = WriterHelper.StartSubItem(null, writer);
            SerializeByType<T>(tools, obj, writer);
            WriterHelper.EndSubItem(token, writer);
        }

        private static void SerializeByType<T>(ISerializerObject<T> tools, T value, WriterHelper writer)
        {
            tools.Write(value, writer);
        }

        private static T DeserializeByType<T>(ISerializerObject<T> tools, ReaderHelper source)
        {
            return tools.Read(source);
        }

    }
    public interface ISerializerObject
    {

    }

    public interface ISerializerObject<T> : ISerializerObject
    {
        T Read(ReaderHelper read);
        void Write(T obj, WriterHelper write);
    }

}
