
/// auto genater code ,dot no ediot this scrpit.

using SerializerScript;

namespace SerializerScript.AutoGenaterCode
{

	public static partial class AutoRegisterTool
	{
		static AutoRegisterTool()
		{
			RegisterAll();
		}
		
		static void RegisterAll()
		{
			CostomSerilzaerModel.RegisterISerializerObject(typeof(ABCSSS),new ABCSSS_SerializerTool());
			CostomSerilzaerModel.RegisterISerializerObject(typeof(ABC),new ABC_SerializerTool());
			CostomSerilzaerModel.RegisterISerializerObject(typeof(VInt2),new VInt2_SerializerTool());
			CostomSerilzaerModel.RegisterISerializerObject(typeof(VInt3),new VInt3_SerializerTool());
			CostomSerilzaerModel.RegisterISerializerObject(typeof(UnityEngine.Vector3),new Vector3_SerializerTool());
			CostomSerilzaerModel.RegisterISerializerObject(typeof(UnityEngine.Vector2Int),new Vector2Int_SerializerTool());
	
		}
	}
}
