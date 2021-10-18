using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using System.IO;
using SerializerScript;

namespace SerializerScript.Editor
{
    public static class TestAutoGeneraterCode
    {
        private static string outRegisterPath = Path.Combine(UnityEngine.Application.dataPath, "Scripts/SerializerScript/AutoGeneraterCode/SerializerScriptRegister_AutoGen.cs");
        private static string outPath = Path.Combine(UnityEngine.Application.dataPath, "Scripts/SerializerScript/AutoGeneraterCode/SerializerScript_AutoGen.cs");
        private static string tempPath = Path.Combine(UnityEngine.Application.dataPath, "Scripts/SerializerScript/ScriptTemp/SerializerScriptTemp.txt");
        private static string tempRegisterPath = Path.Combine(UnityEngine.Application.dataPath, "Scripts/SerializerScript/ScriptTemp/SerializerScriptRegisterTemp.txt");
        public static string mScrpitTemp = "";
        public static string mScrpitRegisterTemp = "";

        [MenuItem("Tools/Auto Code Tools/Clear Genater Serializer File")]
        public static void ClearCode()
        {
            if (UnityEditor.EditorApplication.isCompiling)
            {
                UnityEngine.Debug.LogError("ing Compiling,opt failed.");
                return;
            }
            string str = "/// code empty. " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms");
            if (File.Exists(outPath))
            {
                FileStream fileStream = null;
                fileStream = File.Open(outPath, FileMode.Truncate);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(str);
                streamWriter.Flush(); streamWriter.Close();
                fileStream.Close();
                AssetDatabase.Refresh();
            }

            if (File.Exists(outRegisterPath))
            {
                FileStream fileStream = null;
                fileStream = File.Open(outRegisterPath, FileMode.Truncate);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(str);
                streamWriter.Flush(); streamWriter.Close();
                fileStream.Close();
                AssetDatabase.Refresh();
            }
        }


        [MenuItem("Tools/Auto Code Tools/Genater Serializer Code")]
        public static void GenaterCode()
        {
            if (UnityEditor.EditorApplication.isCompiling)
            {
                UnityEngine.Debug.LogError("ing Compiling,opt failed.");
                return;
            }

            StreamReader streamReader = new StreamReader(tempPath);
            mScrpitTemp = streamReader.ReadToEnd();
            streamReader.Close();

            streamReader = new StreamReader(tempRegisterPath);
            mScrpitRegisterTemp = streamReader.ReadToEnd();
            streamReader.Close();

            Dictionary<Type, TypeModel2MetaTypeInfo> mateInfos = AutoGeneraterSerializerTools.GetParsingSerializerType();

            StringBuilder codeBuilder = new StringBuilder();
            codeBuilder.AppendLine("/// auto genater code ,dot no ediot this scrpit.\n");
            codeBuilder.AppendLine("using SerializerScript;");
            codeBuilder.AppendLine("using SerializerScript.Internal;");
            codeBuilder.AppendLine("namespace SerializerScript.AutoGenaterCode\n{");
            foreach (var it in mateInfos.Values)
            {
                CreateSerializerClassCode(mScrpitTemp, codeBuilder, it);
            }
            codeBuilder.AppendLine("}\n");
            FileStream fileStream = null;
            fileStream = File.Open(outPath, FileMode.OpenOrCreate);
            fileStream.SetLength(0);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(codeBuilder.ToString());
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();

            codeBuilder = new StringBuilder();
            CreateSerializerClassRegisterCode(mScrpitRegisterTemp, codeBuilder, mateInfos);
            fileStream = File.Open(outRegisterPath, FileMode.OpenOrCreate);
            fileStream.SetLength(0);
            streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(codeBuilder.ToString());
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();


            AssetDatabase.Refresh();
        }

        private static void CreateSerializerClassRegisterCode(string scrpitTemp, StringBuilder stringBuilder, Dictionary<Type, TypeModel2MetaTypeInfo> mateInfos)
        {
            StringBuilder stringBuilder1 = new StringBuilder();
            foreach (var it in mateInfos)
            {
                GenaterRegisterCode(stringBuilder1, it.Value);
            }

            stringBuilder.Append(scrpitTemp.Replace("REGISTER_ALL_CODE", stringBuilder1.ToString()));
        }

        private static void GenaterRegisterCode(StringBuilder stringBuilder, TypeModel2MetaTypeInfo model2MetaTypeInfo)
        {
            stringBuilder.AppendLine(string.Format(GetTable(3) + "CostomSerilzaerModel.RegisterISerializerObject(typeof({0}),new {1}_SerializerTool());", GetClassFullName(model2MetaTypeInfo), GetClassName(model2MetaTypeInfo)));
        }

        private static void CreateSerializerClassCode(string scrpitTemp, StringBuilder stringBuilder, TypeModel2MetaTypeInfo model2MetaTypeInfo)
        {
            string scriptStr = scrpitTemp.Replace("CLASSTYPENAME", GetClassFullName(model2MetaTypeInfo));
            scriptStr = scriptStr.Replace("CLASSTYPE_NAME", GetClassName(model2MetaTypeInfo));
            string str = GenaterReadFieldsCode(model2MetaTypeInfo);
            scriptStr = scriptStr.Replace("READ_CASE_LOGIC", str);
            str = GenaterWriteFieldsCode(model2MetaTypeInfo);
            scriptStr = scriptStr.Replace("WRITE_LOGIC", str);
            stringBuilder.Append(scriptStr);

        }

        private static string GetClassFullName(Type type)
        {
            return type.FullName;
        }
        private static string GetClassFullName(TypeModel2MetaTypeInfo model2MetaTypeInfo)
        {
            return model2MetaTypeInfo.type.FullName;
        }

        private static string GetClassName(TypeModel2MetaTypeInfo model2MetaTypeInfo)
        {
            return model2MetaTypeInfo.type.Name;
        }

        private static string GenaterWriteFieldsCode(TypeModel2MetaTypeInfo model2MetaTypeInfo)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < model2MetaTypeInfo.feilds.Count; i++)
            {
                GenaterWriteFieldCode(stringBuilder, model2MetaTypeInfo.feilds[i]);
            }

            return stringBuilder.ToString();
        }

        private static string GenaterReadFieldsCode(TypeModel2MetaTypeInfo model2MetaTypeInfo)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < model2MetaTypeInfo.feilds.Count; i++)
            {
                stringBuilder.AppendLine(string.Format("\t\t\t\tcase {0}:", model2MetaTypeInfo.feilds[i].serializerNumber));
                stringBuilder.AppendLine("\t\t\t\t\t" + GenaterReadFieldCode(model2MetaTypeInfo.feilds[i]));
                stringBuilder.AppendLine("\t\t\t\t\tbreak;");
            }

            return stringBuilder.ToString();
        }

        static Dictionary<int, string> tableStrCache = new Dictionary<int, string>();

        private static string GetTable(int num)
        {
            string str = "";
            if (tableStrCache.TryGetValue(num, out str))
            {
                return str;
            }
            for (int i = 0; i < num; i++)
                str += "\t";

            tableStrCache[num] = str;
            return str;
        }

        private static string GenaterReadFieldCode(TypeModel2MetaFeildInfo model2MetaTypeInfo)
        {
            Type listType = typeof(System.Collections.IList);
            if (IsBaseType(model2MetaTypeInfo.type))
            {
                if (model2MetaTypeInfo.type.IsEnum)
                    return string.Format("ret.{0} = ({2}){1};", model2MetaTypeInfo.filedName, GetReadBase(model2MetaTypeInfo.type), model2MetaTypeInfo.type.FullName);
                else
                    return string.Format("ret.{0} = {1};", model2MetaTypeInfo.filedName, GetReadBase(model2MetaTypeInfo.type));
            }
            else if (listType.IsAssignableFrom(model2MetaTypeInfo.type))
            {
                var argType = model2MetaTypeInfo.type.GetGenericArguments()[0];

                if (listType.IsAssignableFrom(argType))
                {
                    return "Not supported list<list>.";
                }

                if (!IsBaseType(argType))
                {
                    string str = "{\n";
                    str += GetTable(6) + "int fieldNumber = read.FieldNumber;\n";
                    str += string.Format("{0}do\n{1}{{\n", GetTable(6), GetTable(6));
                    str += GetTable(7) + "var token = ReaderHelper.StartSubItem(read);\n";
                    str += string.Format("{1}{0} temp = default({0});\n", GetClassFullName(argType), GetTable(7));
                    str += string.Format("{1}temp = CostomSerilzaerModel.Read<{0}>(read);\n", GetClassFullName(argType), GetTable(7));
                    str += GetTable(7) + "ReaderHelper.EndSubItem(token, read);\n";
                    str += string.Format("{1}ret.{0}.Add(temp);\n", model2MetaTypeInfo.filedName, GetTable(7));
                    str += GetTable(6) + "} while (read.TryReadFieldHeader(fieldNumber));\n";
                    str += GetTable(5) + "}";
                    return str;
                }
                else
                {
                    string str = "{\n";
                    str += GetTable(6) + "int fieldNumber = read.FieldNumber;\n";
                    str += string.Format("{0}do\n{1}{{\n", GetTable(6), GetTable(6));
                    if (argType.IsEnum)
                        str += string.Format("{2}ret.{0}.Add((int){1});\n", model2MetaTypeInfo.filedName, GetReadBase(argType), GetTable(7));
                    else
                        str += string.Format("{2}ret.{0}.Add({1});\n", model2MetaTypeInfo.filedName, GetReadBase(argType), GetTable(7));
                    str += GetTable(6) + "} while (read.TryReadFieldHeader(fieldNumber));\n";
                    str += GetTable(5) + "}";
                    return str;
                }

            }
            else
            {
                return string.Format("ret.{0} = CostomSerilzaerModel.ReadObjectInternal<{1}>(read);", model2MetaTypeInfo.filedName, model2MetaTypeInfo.type);
            }

            return "///read Todo: " + model2MetaTypeInfo.type.FullName.Replace(".", "_");
        }

        private static bool IsBaseType(Type type)
        {
            return type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(string)
                || type == typeof(sbyte)
                || type == typeof(byte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(bool)
                || type.IsEnum
                ;
        }

        private static string GetReadBase(Type type)
        {
            if (type == typeof(int))
            {
                return "read.ReadInt32()";
            }
            else if (type == typeof(uint))
            {
                return "read.ReadUInt32()";
            }
            else if (type == typeof(long))
            {
                return "read.ReadInt64()";
            }
            else if (type == typeof(ulong))
            {
                return "read.ReadUInt64()";
            }
            else if (type == typeof(float))
            {
                return "read.ReadSingle()";
            }
            else if (type == typeof(double))
            {
                return "read.ReadDouble()";
            }
            else if (type == typeof(string))
            {
                return "read.ReadString()";
            }
            else if (type == typeof(sbyte))
            {
                return "read.ReadSByte()";
            }
            else if (type == typeof(byte))
            {
                return "read.ReadByte()";
            }
            else if (type == typeof(short))
            {
                return "read.ReadInt16()";
            }
            else if (type == typeof(ushort))
            {
                return "read.ReadUInt16()";
            }
            else if (type == typeof(bool))
            {
                return "read.ReadBoolean()";
            }
            else if (type.IsEnum)
            {
                return "read.ReadInt32()";
            }

            return string.Format("not supper read {0}.", type.FullName.Replace(".", "_"));
        }

        private static string GetWriteType(Type type)
        {
            if (type == typeof(int))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(uint))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(long))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(ulong))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(float))
            {
                return "WireType.Fixed32";
            }
            else if (type == typeof(double))
            {
                return "WireType.Fixed64";
            }
            else if (type == typeof(string))
            {
                return "WireType.String";
            }
            else if (type == typeof(sbyte))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(byte))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(short))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(ushort))
            {
                return "WireType.Variant";
            }
            else if (type == typeof(bool))
            {
                return "WireType.Variant";
            }
            else if (type.IsEnum)
            {
                return "WireType.Variant";
            }

            return string.Format("not supper write type {0}.", type.FullName.Replace(".", "_"));
        }

        private static string GetWriteBase(Type type)
        {
            if (type == typeof(int))
            {
                return "SerializerScript.Internal.WriterHelper.WriteInt32";
            }
            else if (type == typeof(uint))
            {
                return "SerializerScript.Internal.WriterHelper.WriteUInt32";
            }
            else if (type == typeof(long))
            {
                return "SerializerScript.Internal.WriterHelper.WriteInt64";
            }
            else if (type == typeof(ulong))
            {
                return "SerializerScript.Internal.WriterHelper.WriteUInt64";
            }
            else if (type == typeof(float))
            {
                return "SerializerScript.Internal.WriterHelper.WriteSingle";
            }
            else if (type == typeof(double))
            {
                return "SerializerScript.Internal.WriterHelper.WriteDouble";
            }
            else if (type == typeof(string))
            {
                return "SerializerScript.Internal.WriterHelper.WriteString";
            }
            else if (type == typeof(sbyte))
            {
                return "SerializerScript.Internal.WriterHelper.WriteSByte";
            }
            else if (type == typeof(byte))
            {
                return "SerializerScript.Internal.WriterHelper.WriteByte";
            }
            else if (type == typeof(short))
            {
                return "SerializerScript.Internal.WriterHelper.WriteInt16";
            }
            else if (type == typeof(ushort))
            {
                return "SerializerScript.Internal.WriterHelper.WriteUInt16";
            }
            else if (type == typeof(bool))
            {
                return "SerializerScript.Internal.WriterHelper.WriteBoolean";
            }
            else if (type.IsEnum)
            {
                return "SerializerScript.Internal.WriterHelper.WriteInt32";
            }

            return string.Format("not supper write {0}.", type.FullName.Replace(".", "_"));
        }

        private static void GenaterWriteFieldCode(StringBuilder stringBuilder, TypeModel2MetaFeildInfo model2MetaTypeInfo)
        {
            Type listType = typeof(System.Collections.IList);
            if (IsBaseType(model2MetaTypeInfo.type))
            {
                stringBuilder.AppendFormat("\t\tSerializerScript.Internal.WriterHelper.WriteFieldHeader({0}, {1}, write);\n", model2MetaTypeInfo.serializerNumber, GetWriteType(model2MetaTypeInfo.type));

                if (model2MetaTypeInfo.type.IsEnum)
                    stringBuilder.AppendFormat("\t\t{1}((int)obj.{0}, write);\n", model2MetaTypeInfo.filedName, GetWriteBase(model2MetaTypeInfo.type));
                else
                    stringBuilder.AppendFormat("\t\t{1}(obj.{0}, write);\n", model2MetaTypeInfo.filedName, GetWriteBase(model2MetaTypeInfo.type));
            }
            else if (listType.IsAssignableFrom(model2MetaTypeInfo.type))
            {
                var argType = model2MetaTypeInfo.type.GetGenericArguments()[0];

                if (listType.IsAssignableFrom(argType))
                {
                    stringBuilder.AppendLine("Not supported list<list>.");
                    return;
                }


                if (!IsBaseType(argType))
                {
                    string str = GetTable(2) + "{\n";
                    str += string.Format(GetTable(3) + "var objLists = obj.{0};\n", model2MetaTypeInfo.filedName);
                    str += string.Format(GetTable(3) + "for (int i = 0; i < objLists.Count; i++)\n");
                    str += GetTable(3) + "{\n";
                 
                    str += string.Format(GetTable(4) + "SerializerScript.Internal.WriterHelper.WriteFieldHeader({0}, WireType.String, write);\n", model2MetaTypeInfo.serializerNumber);
                    str += GetTable(4) + "var token = SerializerScript.Internal.WriterHelper.StartSubItem(objLists[i], write);\n";
                    str += GetTable(4) + "if(objLists[i] != null)\n";
                    str += GetTable(5) + "CostomSerilzaerModel.Write(objLists[i], write);\n";
                    str += GetTable(4) + "SerializerScript.Internal.WriterHelper.EndSubItem(token, write);\n";
                    str += GetTable(3) + "}\n";
                    str += GetTable(2) + "}\n";
                    stringBuilder.AppendLine(str);
                }
                else
                {
                    string str = GetTable(2) + "{\n";
                    str += string.Format(GetTable(3) + "var objLists = obj.{0};\n", model2MetaTypeInfo.filedName);
                    str += GetTable(3) + "for (int i = 0; i < objLists.Count; i++)\n";
                    str += GetTable(3) + "{\n";
                    str += GetTable(4) + string.Format("SerializerScript.Internal.WriterHelper.WriteFieldHeader({0}, {1}, write);\n", model2MetaTypeInfo.serializerNumber, GetWriteType(argType));
                    if (argType.IsEnum)
                        str += GetTable(4) + string.Format("{0}((int)objLists[i], write);\n", GetWriteBase(argType));
                    else
                        str += GetTable(4) + string.Format("{0}(objLists[i], write);\n", GetWriteBase(argType));

                    str += GetTable(3) + "}\n";
                    str += GetTable(2) + "}\n";
                    stringBuilder.AppendLine(str);
                }
            }
            else //自定义类型
            {
                string str = ""; 
                str += string.Format("\n" + GetTable(2) + "if(obj.{0} != null)\n", model2MetaTypeInfo.filedName);
                str += GetTable(2) + "{\n";
                str += string.Format(GetTable(3) + "SerializerScript.Internal.WriterHelper.WriteFieldHeader({0},WireType.StartGroup, write);\n", model2MetaTypeInfo.serializerNumber);
                str += string.Format(GetTable(3) + "CostomSerilzaerModel.WriteObjectInternal(obj.{0}, write);\n", model2MetaTypeInfo.filedName);
                str += GetTable(2) + "}\n";
                stringBuilder.AppendLine(str);
            }

        }
    }


}