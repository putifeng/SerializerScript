
基于protobuffer源码修改的高度自定义序列化工具

为什么用这个:
    如果对性能没有要求的,可以使用protobuffer原生的序列化
    起因是protobuffer对于自定义class的序列化都是需要通过反射来执行,配置等高频率的消耗吃不消,故而诞生本项目

作用:
    可用于配置/对象传输,生成序列化二进制代码的场景,避免手动写序列化的代码,
    
ProtoRead/ProtoWrite改名原因:
    接入项目的时候,避免和原有项目的协议序列化的冲突,可能使用的版本不一样导致一些报错,且删减了大部分代码,只会有一些基本操作留下,


--------------------------------------------------------------
案例1:

[AutoGeneraterSerializerCode]
public class ABCSSS
{
    [SerializerNumber(1)]
    public int a;
    [SerializerNumber(2)]
    public int b;
}



使用步骤:
ABCSSS bc = new ABCSSS();
byte[] bytes = ProtobufferHelper.Serilaizer(bC);
ABCSSS b2 = ProtobufferHelper.Deserilaizer<ABC>(bytes);

 
--------------------------------------------------------------


案例2:
注册Unityengine.vector3 序列化代码生成

RegisterCostomSerializerTypeHandler(typeof(UnityEngine.Vector3)); //自动收集public 的filed 字段生成

--------------------------------------------------------------


案例3:
注册 VInt2 序列化代码生成,手动指定该类型需要序列化字段的number值,字段名,字段类型

RegisterCostomSerializerTypeHandler(GetVint3MateTypeInfo);

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
		
--------------------------------------------------------------
:====================================================
注意:
:   新增 AutoGeneraterSerializerCode 和SerializerNumber /或者 手动 注册 序列化代码生成
:	需要生成序列化代码,点击 Tools/Auto Code Tools/Genater Serializer Code; 生成,
：	如果 SerializerScriptRegister_AutoGen.cs/SerializerScript_AutoGen.cs 报错,可以先清除 Tools/Auto Code Tools/Clear Genater Serializer File,等待编译完成,再 Tools/Auto Code Tools/Genater Serializer Code
:====================================================
