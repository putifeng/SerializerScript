
using System;
using System.Collections.Generic;
using System.Linq;
using SerializerScript.Debug;

namespace SerializerScript
{
    public static partial class AutoGeneraterSerializerTools
    {
        static List<Func<TypeModel2MetaTypeInfo>> registerHandler;

        private static void InitHandler()
        {
            registerHandler = new List<Func<TypeModel2MetaTypeInfo>>();
            RegisterCostomSerializerTypeHandler(GetVint2MateTypeInfo);
            RegisterCostomSerializerTypeHandler(GetVint3MateTypeInfo);
            RegisterCostomSerializerTypeHandler(typeof(UnityEngine.Vector3));
            RegisterCostomSerializerTypeHandler(typeof(UnityEngine.Vector2Int), true);
        }


        /// <summary>
        /// 需要填充生成的类型的,类型里面的字段名字/类型/序列化编号
        /// </summary>
        /// <param name="Properties">是否生成类属性字段</param>
        /// <returns></returns>
        private static void RegisterCostomSerializerTypeHandler(Func<TypeModel2MetaTypeInfo> func)
        {
            registerHandler.Add(func);
        }

        /// <summary>
        /// 依赖反射来生成字段,会收集所有的public/instace字段(不会收集属性), 序列化编号会自动生成,
        /// </summary>
        private static void RegisterCostomSerializerTypeHandler(Type type, bool genProperties = false)
        {
            registerHandler.Add(() =>
            {
                return GeneraterMateInfo(type, genProperties);
            });
        }

        private static TypeModel2MetaTypeInfo GeneraterMateInfo(Type type, bool genProperties)
        {
            TypeModel2MetaTypeInfo typeInfo = new TypeModel2MetaTypeInfo();
            int serializerNumberBase = 1;
            {
                var fileds = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                typeInfo.type = type;

                for (int i = 0; i < fileds.Length; i++)
                {
                    TypeModel2MetaFeildInfo feildInfo = new TypeModel2MetaFeildInfo();
                    feildInfo.type = fileds[i].FieldType;
                    feildInfo.serializerNumber = serializerNumberBase++;
                    feildInfo.filedName = fileds[i].Name;
                    typeInfo.feilds.Add(feildInfo);
                }
            }

            if (genProperties)
            {
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var d2 = type.GetDefaultMembers();
                var defaultMambers = d2.ToList();
                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i].CanWrite == false)
                        continue;

                    if (properties[i].CanRead == false)
                        continue;

                    if (defaultMambers.Find(a => a.Name == properties[i].Name) != null)
                        continue;

                    TypeModel2MetaFeildInfo feildInfo = new TypeModel2MetaFeildInfo();
                    feildInfo.type = properties[i].PropertyType;
                    feildInfo.serializerNumber = serializerNumberBase++;
                    feildInfo.filedName = properties[i].Name;
                    typeInfo.feilds.Add(feildInfo);
                }
            }

            return typeInfo;
        }

        private static TypeModel2MetaTypeInfo GetVint3MateTypeInfo()
        {
            TypeModel2MetaFeildInfo feildInfo = null;
            Type insertType = typeof(VInt3);
            VInt3 vInt2Temp = new VInt3();
            TypeModel2MetaTypeInfo typeInfo = new TypeModel2MetaTypeInfo();
            typeInfo.type = insertType;

            int serializerNumber = 1;

            feildInfo = new TypeModel2MetaFeildInfo();
            feildInfo.type = vInt2Temp.x.GetType();
            feildInfo.filedName = "x";
            feildInfo.serializerNumber = serializerNumber++;
            typeInfo.feilds.Add(feildInfo);

            feildInfo = new TypeModel2MetaFeildInfo();
            feildInfo.type = vInt2Temp.y.GetType();
            feildInfo.filedName = "y";
            feildInfo.serializerNumber = serializerNumber++;
            typeInfo.feilds.Add(feildInfo);

            feildInfo = new TypeModel2MetaFeildInfo();
            feildInfo.type = vInt2Temp.z.GetType();
            feildInfo.filedName = "z";
            feildInfo.serializerNumber = serializerNumber++;
            typeInfo.feilds.Add(feildInfo);

            return typeInfo;
        }

        private static TypeModel2MetaTypeInfo GetVint2MateTypeInfo()
        {
            TypeModel2MetaFeildInfo feildInfo = null;
            Type insertType = typeof(VInt2);
            VInt2 vInt2Temp = new VInt2();
            TypeModel2MetaTypeInfo typeInfo = new TypeModel2MetaTypeInfo();
            typeInfo.type = insertType;
            int serializerNumber = 1;

            feildInfo = new TypeModel2MetaFeildInfo();
            feildInfo.type = vInt2Temp.x.GetType();
            feildInfo.filedName = "x";
            feildInfo.serializerNumber = serializerNumber++;
            typeInfo.feilds.Add(feildInfo);

            feildInfo = new TypeModel2MetaFeildInfo();
            feildInfo.type = vInt2Temp.y.GetType();
            feildInfo.filedName = "y";
            feildInfo.serializerNumber = serializerNumber++;
            typeInfo.feilds.Add(feildInfo);

            return typeInfo;
        }

        private static bool RegisterCostomSerializerType(Dictionary<Type, TypeModel2MetaTypeInfo> metaInfos)
        {
            bool state = true;
            for (int i = 0; i < registerHandler.Count; i++)
            {
                if (state == false)
                    break;

                var info = registerHandler[i].Invoke();
                if (info.type == null)
                {
                    state = false;
                    DebugHelper.LogError("register gen code failed.serilaizer type is null.");
                    continue;
                }

                if (info.feilds.Count == 0)
                {
                    state = false;
                    DebugHelper.LogError("register gen code failed.serilaizer type feilds.count is zero." + info.type.Name);
                    continue;
                }

                bool checkSucc = true;
                for (int j = 0; j < info.feilds.Count; j++)
                {

                    if (info.feilds[j].type == null)
                    {
                        checkSucc = false;
                        break;
                    }

                    if (info.feilds[j].serializerNumber <= 0)
                    {
                        checkSucc = false;
                        DebugHelper.LogError(string.Format("register gen code failed.serilaizer type feild serializerNumber is zero.{0}", info.feilds[j].type.Name));
                        break;
                    }

                    if (string.IsNullOrEmpty(info.feilds[j].filedName))
                    {
                        DebugHelper.LogError(string.Format("register gen code failed.serilaizer type feild filedName is empty or null.{0}", info.feilds[j].type.Name));
                        checkSucc = false;
                        break;
                    }
                }

                if (checkSucc == false)
                {
                    state = false;
                    continue;
                }

                metaInfos[info.type] = info;
            }
            return state;
        }

        private static TypeModel2MetaTypeInfo ParsingType(Type type)
        {
            System.Reflection.FieldInfo[] fieldInfos = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            TypeModel2MetaTypeInfo metaTypeInfo = new TypeModel2MetaTypeInfo();
            metaTypeInfo.type = type;
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                SerializerNumberAttribute serializerNumber = fieldInfos[i].GetCustomAttribute<SerializerNumberAttribute>();
                if (serializerNumber != null)
                {
                    TypeModel2MetaFeildInfo feildInfo = new TypeModel2MetaFeildInfo();
                    feildInfo.type = fieldInfos[i].FieldType;
                    feildInfo.filedName = fieldInfos[i].Name;
                    feildInfo.serializerNumber = serializerNumber.number;
                    metaTypeInfo.feilds.Add(feildInfo);
                }
            }

            return metaTypeInfo;
        }

        public static Dictionary<Type, TypeModel2MetaTypeInfo> GetParsingSerializerType()
        {
            var types = typeof(SerializerScript.CostomSerilzaerModel).Assembly.GetTypes();
            Dictionary<Type, TypeModel2MetaTypeInfo> metaInfos = new Dictionary<Type, TypeModel2MetaTypeInfo>();

            for (int i = 0; i < types.Length; i++)
            {
                var autoGenerater = types[i].GetCustomAttribute<SerializerScript.AutoGeneraterSerializerCodeAttribute>();
                if (autoGenerater != null)
                {
                    metaInfos[types[i]] = ParsingType(types[i]);
                }
            }

            InitHandler();
            if (RegisterCostomSerializerType(metaInfos) == false)
            {
                return new Dictionary<Type, TypeModel2MetaTypeInfo>();
            }
            return metaInfos;
        }
    }

    /// <summary>
    /// 类型收集集合
    /// </summary>
    public class TypeModel2MetaTypeInfo
    {
        /// <summary>
        /// 需要生成的类型
        /// </summary>
        public Type type;
        /// <summary>
        /// 需要生成的字段集合
        /// </summary>
        public List<TypeModel2MetaFeildInfo> feilds = new List<TypeModel2MetaFeildInfo>();
    }

    /// <summary>
    /// 字段类型收集集合
    /// </summary>
    public class TypeModel2MetaFeildInfo
    {

        /// <summary>
        /// 字段的类型
        /// </summary>
        public Type type;

        /// <summary>
        /// 字段序列化的编号
        /// </summary>
        public int serializerNumber;

        /// <summary>
        /// 字段名字
        /// </summary>
        public string filedName;
    }

}