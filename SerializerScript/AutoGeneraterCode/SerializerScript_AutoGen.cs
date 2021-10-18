/// auto genater code ,dot no ediot this scrpit.

using SerializerScript;
using SerializerScript.Internal;
namespace SerializerScript.AutoGenaterCode
{

public class ABCSSS_SerializerTool : SerializerScript.ISerializerObject<ABCSSS>
{
	public ABCSSS Read(SerializerScript.Internal.ReaderHelper read)
	{
		bool isNew = false;
		ABCSSS ret = default(ABCSSS);
		int field = 0;
		while ((field = read.ReadFieldHeader()) > 0)
		{
			if (isNew == false)
			{
				ret = new ABCSSS();
				isNew = true;
			} 
				
			switch (field) 
 			{
				case 1:
					ret.a = read.ReadInt32();
					break;
				case 2:
					ret.b = read.ReadInt32();
					break;
				case 3:
					ret.c = read.ReadSingle();
					break;
				case 4:
					ret.a1 = read.ReadDouble();
					break;
				case 5:
					ret.a2 = read.ReadUInt64();
					break;
				case 6:
					ret.a3 = read.ReadInt64();
					break;
				case 7:
					ret.a4 = read.ReadSByte();
					break;
				case 8:
					ret.a5 = read.ReadByte();
					break;
				case 9:
					ret.a6 = read.ReadInt16();
					break;
				case 10:
					ret.a7 = read.ReadUInt16();
					break;
				case 12:
					ret.a8 = read.ReadBoolean();
					break;
				case 13:
					ret.a9 = read.ReadBoolean();
					break;
				case 15:
					ret.eenumA = (EenumA)read.ReadInt32();
					break;

				default:
					read.SkipField();
					break;  			
			} 
		} 
 		return ret;
	}
	
	public void Write(ABCSSS obj, SerializerScript.Internal.WriterHelper write)
	{
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(1, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.a, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(2, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.b, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(3, WireType.Fixed32, write);
		SerializerScript.Internal.WriterHelper.WriteSingle(obj.c, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(4, WireType.Fixed64, write);
		SerializerScript.Internal.WriterHelper.WriteDouble(obj.a1, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(5, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteUInt64(obj.a2, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(6, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt64(obj.a3, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(7, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteSByte(obj.a4, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(8, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteByte(obj.a5, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(9, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt16(obj.a6, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(10, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteUInt16(obj.a7, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(12, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteBoolean(obj.a8, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(13, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteBoolean(obj.a9, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(15, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32((int)obj.eenumA, write);

	}
}

public class ABC_SerializerTool : SerializerScript.ISerializerObject<ABC>
{
	public ABC Read(SerializerScript.Internal.ReaderHelper read)
	{
		bool isNew = false;
		ABC ret = default(ABC);
		int field = 0;
		while ((field = read.ReadFieldHeader()) > 0)
		{
			if (isNew == false)
			{
				ret = new ABC();
				isNew = true;
			} 
				
			switch (field) 
 			{
				case 3:
					{
						int fieldNumber = read.FieldNumber;
						do
						{
							var token = ReaderHelper.StartSubItem(read);
							ABCSSS temp = default(ABCSSS);
							temp = CostomSerilzaerModel.Read<ABCSSS>(read);
							ReaderHelper.EndSubItem(token, read);
							ret.BCSSSes.Add(temp);
						} while (read.TryReadFieldHeader(fieldNumber));
					}
					break;
				case 2:
					{
						int fieldNumber = read.FieldNumber;
						do
						{
							ret.baseAbc.Add(read.ReadInt32());
						} while (read.TryReadFieldHeader(fieldNumber));
					}
					break;
				case 1:
					ret.abcsss = CostomSerilzaerModel.ReadObjectInternal<ABCSSS>(read);
					break;
				case 5:
					ret.teststr = read.ReadString();
					break;
				case 6:
					ret.aBC = CostomSerilzaerModel.ReadObjectInternal<ABC>(read);
					break;

				default:
					read.SkipField();
					break;  			
			} 
		} 
 		return ret;
	}
	
	public void Write(ABC obj, SerializerScript.Internal.WriterHelper write)
	{
		{
			var objLists = obj.BCSSSes;
			for (int i = 0; i < objLists.Count; i++)
			{
				SerializerScript.Internal.WriterHelper.WriteFieldHeader(3, WireType.String, write);
				var token = SerializerScript.Internal.WriterHelper.StartSubItem(objLists[i], write);
				if(objLists[i] != null)
					CostomSerilzaerModel.Write(objLists[i], write);
				SerializerScript.Internal.WriterHelper.EndSubItem(token, write);
			}
		}

		{
			var objLists = obj.baseAbc;
			for (int i = 0; i < objLists.Count; i++)
			{
				SerializerScript.Internal.WriterHelper.WriteFieldHeader(2, WireType.Variant, write);
				SerializerScript.Internal.WriterHelper.WriteInt32(objLists[i], write);
			}
		}


		if(obj.abcsss != null)
		{
			SerializerScript.Internal.WriterHelper.WriteFieldHeader(1,WireType.StartGroup, write);
			CostomSerilzaerModel.WriteObjectInternal(obj.abcsss, write);
		}

		SerializerScript.Internal.WriterHelper.WriteFieldHeader(5, WireType.String, write);
		SerializerScript.Internal.WriterHelper.WriteString(obj.teststr, write);

		if(obj.aBC != null)
		{
			SerializerScript.Internal.WriterHelper.WriteFieldHeader(6,WireType.StartGroup, write);
			CostomSerilzaerModel.WriteObjectInternal(obj.aBC, write);
		}


	}
}

public class VInt2_SerializerTool : SerializerScript.ISerializerObject<VInt2>
{
	public VInt2 Read(SerializerScript.Internal.ReaderHelper read)
	{
		bool isNew = false;
		VInt2 ret = default(VInt2);
		int field = 0;
		while ((field = read.ReadFieldHeader()) > 0)
		{
			if (isNew == false)
			{
				ret = new VInt2();
				isNew = true;
			} 
				
			switch (field) 
 			{
				case 1:
					ret.x = read.ReadInt32();
					break;
				case 2:
					ret.y = read.ReadInt32();
					break;

				default:
					read.SkipField();
					break;  			
			} 
		} 
 		return ret;
	}
	
	public void Write(VInt2 obj, SerializerScript.Internal.WriterHelper write)
	{
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(1, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.x, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(2, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.y, write);

	}
}

public class VInt3_SerializerTool : SerializerScript.ISerializerObject<VInt3>
{
	public VInt3 Read(SerializerScript.Internal.ReaderHelper read)
	{
		bool isNew = false;
		VInt3 ret = default(VInt3);
		int field = 0;
		while ((field = read.ReadFieldHeader()) > 0)
		{
			if (isNew == false)
			{
				ret = new VInt3();
				isNew = true;
			} 
				
			switch (field) 
 			{
				case 1:
					ret.x = read.ReadInt32();
					break;
				case 2:
					ret.y = read.ReadInt32();
					break;
				case 3:
					ret.z = read.ReadInt32();
					break;

				default:
					read.SkipField();
					break;  			
			} 
		} 
 		return ret;
	}
	
	public void Write(VInt3 obj, SerializerScript.Internal.WriterHelper write)
	{
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(1, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.x, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(2, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.y, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(3, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.z, write);

	}
}

public class Vector3_SerializerTool : SerializerScript.ISerializerObject<UnityEngine.Vector3>
{
	public UnityEngine.Vector3 Read(SerializerScript.Internal.ReaderHelper read)
	{
		bool isNew = false;
		UnityEngine.Vector3 ret = default(UnityEngine.Vector3);
		int field = 0;
		while ((field = read.ReadFieldHeader()) > 0)
		{
			if (isNew == false)
			{
				ret = new UnityEngine.Vector3();
				isNew = true;
			} 
				
			switch (field) 
 			{
				case 1:
					ret.x = read.ReadSingle();
					break;
				case 2:
					ret.y = read.ReadSingle();
					break;
				case 3:
					ret.z = read.ReadSingle();
					break;

				default:
					read.SkipField();
					break;  			
			} 
		} 
 		return ret;
	}
	
	public void Write(UnityEngine.Vector3 obj, SerializerScript.Internal.WriterHelper write)
	{
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(1, WireType.Fixed32, write);
		SerializerScript.Internal.WriterHelper.WriteSingle(obj.x, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(2, WireType.Fixed32, write);
		SerializerScript.Internal.WriterHelper.WriteSingle(obj.y, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(3, WireType.Fixed32, write);
		SerializerScript.Internal.WriterHelper.WriteSingle(obj.z, write);

	}
}

public class Vector2Int_SerializerTool : SerializerScript.ISerializerObject<UnityEngine.Vector2Int>
{
	public UnityEngine.Vector2Int Read(SerializerScript.Internal.ReaderHelper read)
	{
		bool isNew = false;
		UnityEngine.Vector2Int ret = default(UnityEngine.Vector2Int);
		int field = 0;
		while ((field = read.ReadFieldHeader()) > 0)
		{
			if (isNew == false)
			{
				ret = new UnityEngine.Vector2Int();
				isNew = true;
			} 
				
			switch (field) 
 			{
				case 1:
					ret.x = read.ReadInt32();
					break;
				case 2:
					ret.y = read.ReadInt32();
					break;

				default:
					read.SkipField();
					break;  			
			} 
		} 
 		return ret;
	}
	
	public void Write(UnityEngine.Vector2Int obj, SerializerScript.Internal.WriterHelper write)
	{
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(1, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.x, write);
		SerializerScript.Internal.WriterHelper.WriteFieldHeader(2, WireType.Variant, write);
		SerializerScript.Internal.WriterHelper.WriteInt32(obj.y, write);

	}
}
}

