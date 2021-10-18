#define DEBUG
using System;
using System.IO;
using System.Text;

namespace SerializerScript.Internal
{
	public sealed class WriterHelper : IDisposable
	{
		private const int RecursionCheckDepth = 25;

		private Stream dest;

		private int fieldNumber;

		private int flushLock;

		private WireType wireType;

		private int depth = 0;

		private byte[] ioBuffer;

		private int ioIndex;

		private int position;

		private static readonly UTF8Encoding encoding = new UTF8Encoding();

		private int packedFieldNumber;

		internal WireType WireType { get { return wireType; } }


		public static void WriteFieldHeader(int fieldNumber, WireType wireType, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (writer.wireType != WireType.None)
			{
				throw new InvalidOperationException("Cannot write a " + wireType.ToString() + " header until the " + writer.wireType.ToString() + " data has been written");
			}
			if (fieldNumber < 0)
			{
				throw new ArgumentOutOfRangeException("fieldNumber");
			}
			switch (wireType)
			{
			default:
				throw new ArgumentException("Invalid wire-type: " + wireType, "wireType");
			case WireType.Variant:
			case WireType.Fixed64:
			case WireType.String:
			case WireType.StartGroup:
			case WireType.Fixed32:
			case WireType.SignedVariant:
				if (writer.packedFieldNumber == 0)
				{
					writer.fieldNumber = fieldNumber;
					writer.wireType = wireType;
					WriteHeaderCore(fieldNumber, wireType, writer);
					break;
				}
				if (writer.packedFieldNumber == fieldNumber)
				{
					switch (wireType)
					{
					default:
						throw new InvalidOperationException("Wire-type cannot be encoded as packed: " + wireType);
					case WireType.Variant:
					case WireType.Fixed64:
					case WireType.Fixed32:
					case WireType.SignedVariant:
						writer.fieldNumber = fieldNumber;
						writer.wireType = wireType;
						break;
					}
					break;
				}
				throw new InvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber + " but received " + fieldNumber);
			}
		}

		internal static void WriteHeaderCore(int fieldNumber, WireType wireType, WriterHelper writer)
		{
			uint value = (uint)(fieldNumber << 3) | (uint)(wireType & (WireType)7);
			WriteUInt32Variant(value, writer);
		}

		public static void WriteBytes(byte[] data, WriterHelper writer)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			WriteBytes(data, 0, data.Length, writer);
		}

		public static void WriteBytes(byte[] data, int offset, int length, WriterHelper writer)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed32:
				if (length != 4)
				{
					throw new ArgumentException("length");
				}
				break;
			case WireType.Fixed64:
				if (length != 8)
				{
					throw new ArgumentException("length");
				}
				break;
			case WireType.String:
				WriteUInt32Variant((uint)length, writer);
				writer.wireType = WireType.None;
				if (length == 0)
				{
					return;
				}
				if (writer.flushLock != 0 || length <= writer.ioBuffer.Length)
				{
					break;
				}
				Flush(writer);
				writer.dest.Write(data, offset, length);
				writer.position += length;
				return;
			default:
				throw CreateException(writer);
			}
			DemandSpace(length, writer);
			Helpers.BlockCopy(data, offset, writer.ioBuffer, writer.ioIndex, length);
			IncrementedAndReset(length, writer);
		}

		private static void CopyRawFromStream(Stream source, WriterHelper writer)
		{
			byte[] array = writer.ioBuffer;
			int num = array.Length - writer.ioIndex;
			int num2 = 1;
			while (num > 0 && (num2 = source.Read(array, writer.ioIndex, num)) > 0)
			{
				writer.ioIndex += num2;
				writer.position += num2;
				num -= num2;
			}
			if (num2 <= 0)
			{
				return;
			}
			if (writer.flushLock == 0)
			{
				Flush(writer);
				while ((num2 = source.Read(array, 0, array.Length)) > 0)
				{
					writer.dest.Write(array, 0, num2);
					writer.position += num2;
				}
				return;
			}
			while (true)
			{
				DemandSpace(128, writer);
				if ((num2 = source.Read(writer.ioBuffer, writer.ioIndex, writer.ioBuffer.Length - writer.ioIndex)) <= 0)
				{
					break;
				}
				writer.position += num2;
				writer.ioIndex += num2;
				bool flag = true;
			}
		}

		private static void IncrementedAndReset(int length, WriterHelper writer)
		{
			Helpers.DebugAssert(length >= 0);
			writer.ioIndex += length;
			writer.position += length;
			writer.wireType = WireType.None;
		}

		public static SubItemToken StartSubItem(object instance, WriterHelper writer)
		{
			return StartSubItem(instance, writer, allowFixed: false);
		}

		private static SubItemToken StartSubItem(object instance, WriterHelper writer, bool allowFixed)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (++writer.depth > RecursionCheckDepth)
			{
				throw new InvalidOperationException(instance.GetType().FullName + ":writer.depth > " + RecursionCheckDepth);
			}

			if (writer.packedFieldNumber != 0)
			{
				throw new InvalidOperationException("Cannot begin a sub-item while performing packed encoding");
			}
			switch (writer.wireType)
			{
			case WireType.StartGroup:
				writer.wireType = WireType.None;
				return new SubItemToken(-writer.fieldNumber);
			case WireType.String:
				writer.wireType = WireType.None;
				DemandSpace(32, writer);
				writer.flushLock++;
				writer.position++;
				return new SubItemToken(writer.ioIndex++);
			case WireType.Fixed32:
			{
				if (!allowFixed)
				{
					throw CreateException(writer);
				}
				DemandSpace(32, writer);
				writer.flushLock++;
				SubItemToken result = new SubItemToken(writer.ioIndex);
				IncrementedAndReset(4, writer);
				return result;
			}
			default:
				throw CreateException(writer);
			}
		}

		public static void EndSubItem(SubItemToken token, WriterHelper writer)
		{
			EndSubItem(token, writer, PrefixStyle.Base128);
		}

		private static void EndSubItem(SubItemToken token, WriterHelper writer, PrefixStyle style)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (writer.wireType != WireType.None)
			{
				throw CreateException(writer);
			}
			int value = token.value;
			if (writer.depth <= 0)
			{
				throw CreateException(writer);
			}

			if (writer.depth-- > RecursionCheckDepth)
			{
				throw CreateException(writer);
			}

			writer.packedFieldNumber = 0;
			if (value < 0)
			{
				WriteHeaderCore(-value, WireType.EndGroup, writer);
				writer.wireType = WireType.None;
				return;
			}
			switch (style)
			{
			case PrefixStyle.Fixed32:
			{
				int num = writer.ioIndex - value - 4;
				WriteInt32ToBuffer(num, writer.ioBuffer, value);
				break;
			}
			case PrefixStyle.Fixed32BigEndian:
			{
				int num = writer.ioIndex - value - 4;
				byte[] array2 = writer.ioBuffer;
				WriteInt32ToBuffer(num, array2, value);
				byte b = array2[value];
				array2[value] = array2[value + 3];
				array2[value + 3] = b;
				b = array2[value + 1];
				array2[value + 1] = array2[value + 2];
				array2[value + 2] = b;
				break;
			}
			case PrefixStyle.Base128:
			{
				int num = writer.ioIndex - value - 1;
				int num2 = 0;
				uint num3 = (uint)num;
				while ((num3 >>= 7) != 0)
				{
					num2++;
				}
				if (num2 == 0)
				{
					writer.ioBuffer[value] = (byte)((uint)num & 0x7Fu);
					break;
				}
				DemandSpace(num2, writer);
				byte[] array = writer.ioBuffer;
				Helpers.BlockCopy(array, value + 1, array, value + 1 + num2, num);
				num3 = (uint)num;
				do
				{
					array[value++] = (byte)((num3 & 0x7Fu) | 0x80u);
				}
				while ((num3 >>= 7) != 0);
				array[value - 1] = (byte)(array[value - 1] & 0xFFFFFF7Fu);
				writer.position += num2;
				writer.ioIndex += num2;
				break;
			}
			default:
				throw new ArgumentOutOfRangeException("style");
			}
			if (--writer.flushLock == 0 && writer.ioIndex >= 1024)
			{
				Flush(writer);
			}
		}

		public WriterHelper(Stream dest)
		{
			if (dest == null)
			{
				throw new ArgumentNullException("dest");
			}
			if (!dest.CanWrite)
			{
				throw new ArgumentException("Cannot write to stream", "dest");
			}
			this.dest = dest;
			ioBuffer = BufferPool.GetBuffer();
			wireType = WireType.None;
		
		}

		void IDisposable.Dispose()
		{
			Dispose();
		}

		private void Dispose()
		{
			if (dest != null)
			{
				Flush(this);
				dest = null;
			}
			BufferPool.ReleaseBufferToPool(ref ioBuffer);
		}

		internal static int GetPosition(WriterHelper writer)
		{
			return writer.position;
		}

		private static void DemandSpace(int required, WriterHelper writer)
		{
			if (writer.ioBuffer.Length - writer.ioIndex >= required)
			{
				return;
			}
			if (writer.flushLock == 0)
			{
				Flush(writer);
				if (writer.ioBuffer.Length - writer.ioIndex >= required)
				{
					return;
				}
			}
			BufferPool.ResizeAndFlushLeft(ref writer.ioBuffer, required + writer.ioIndex, 0, writer.ioIndex);
		}

		public void Close()
		{
			if (depth != 0 || flushLock != 0)
			{
				throw new InvalidOperationException("Unable to close stream in an incomplete state");
			}
			Dispose();
		}

		internal void CheckDepthFlushlock()
		{
			if (depth != 0 || flushLock != 0)
			{
				throw new InvalidOperationException("The writer is in an incomplete state");
			}
		}

		internal static void Flush(WriterHelper writer)
		{
			if (writer.flushLock == 0 && writer.ioIndex != 0)
			{
				writer.dest.Write(writer.ioBuffer, 0, writer.ioIndex);
				writer.ioIndex = 0;
			}
		}

		private static void WriteUInt32Variant(uint value, WriterHelper writer)
		{
			DemandSpace(5, writer);
			int num = 0;
			do
			{
				writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7Fu) | 0x80u);
				num++;
			}
			while ((value >>= 7) != 0);
			writer.ioBuffer[writer.ioIndex - 1] &= 127;
			writer.position += num;
		}

		internal static uint Zig(int value)
		{
			return (uint)((value << 1) ^ (value >> 31));
		}

		internal static ulong Zig(long value)
		{
			return (ulong)((value << 1) ^ (value >> 63));
		}

		private static void WriteUInt64Variant(ulong value, WriterHelper writer)
		{
			DemandSpace(10, writer);
			int num = 0;
			do
			{
				writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7F) | 0x80);
				num++;
			}
			while ((value >>= 7) != 0);
			writer.ioBuffer[writer.ioIndex - 1] &= 127;
			writer.position += num;
		}

		public static void WriteString(string value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (writer.wireType != WireType.String)
			{
				throw CreateException(writer);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length == 0)
			{
				WriteUInt32Variant(0u, writer);
				writer.wireType = WireType.None;
				return;
			}
			int byteCount = encoding.GetByteCount(value);
			WriteUInt32Variant((uint)byteCount, writer);
			DemandSpace(byteCount, writer);
			int bytes = encoding.GetBytes(value, 0, value.Length, writer.ioBuffer, writer.ioIndex);
			Helpers.DebugAssert(byteCount == bytes);
			IncrementedAndReset(bytes, writer);
		}

		public static void WriteUInt64(ulong value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed64:
				WriteInt64((long)value, writer);
				break;
			case WireType.Variant:
				WriteUInt64Variant(value, writer);
				writer.wireType = WireType.None;
				break;
			case WireType.Fixed32:
				WriteUInt32(checked((uint)value), writer);
				break;
			default:
				throw CreateException(writer);
			}
		}

		public static void WriteInt64(long value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed64:
			{
				DemandSpace(8, writer);
				byte[] array = writer.ioBuffer;
				int num = writer.ioIndex;
				array[num] = (byte)value;
				array[num + 1] = (byte)(value >> 8);
				array[num + 2] = (byte)(value >> 16);
				array[num + 3] = (byte)(value >> 24);
				array[num + 4] = (byte)(value >> 32);
				array[num + 5] = (byte)(value >> 40);
				array[num + 6] = (byte)(value >> 48);
				array[num + 7] = (byte)(value >> 56);
				IncrementedAndReset(8, writer);
				break;
			}
			case WireType.SignedVariant:
				WriteUInt64Variant(Zig(value), writer);
				writer.wireType = WireType.None;
				break;
			case WireType.Variant:
			{
				if (value >= 0)
				{
					WriteUInt64Variant((ulong)value, writer);
					writer.wireType = WireType.None;
					break;
				}
				DemandSpace(10, writer);
				byte[] array = writer.ioBuffer;
				int num = writer.ioIndex;
				array[num] = (byte)(value | 0x80);
				array[num + 1] = (byte)((uint)(int)(value >> 7) | 0x80u);
				array[num + 2] = (byte)((uint)(int)(value >> 14) | 0x80u);
				array[num + 3] = (byte)((uint)(int)(value >> 21) | 0x80u);
				array[num + 4] = (byte)((uint)(int)(value >> 28) | 0x80u);
				array[num + 5] = (byte)((uint)(int)(value >> 35) | 0x80u);
				array[num + 6] = (byte)((uint)(int)(value >> 42) | 0x80u);
				array[num + 7] = (byte)((uint)(int)(value >> 49) | 0x80u);
				array[num + 8] = (byte)((uint)(int)(value >> 56) | 0x80u);
				array[num + 9] = 1;
				IncrementedAndReset(10, writer);
				break;
			}
			case WireType.Fixed32:
				WriteInt32(checked((int)value), writer);
				break;
			default:
				throw CreateException(writer);
			}
		}

		public static void WriteUInt32(uint value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed32:
				WriteInt32((int)value, writer);
				break;
			case WireType.Fixed64:
				WriteInt64((int)value, writer);
				break;
			case WireType.Variant:
				WriteUInt32Variant(value, writer);
				writer.wireType = WireType.None;
				break;
			default:
				throw CreateException(writer);
			}
		}

		public static void WriteInt16(short value, WriterHelper writer)
		{
			WriteInt32(value, writer);
		}

		public static void WriteUInt16(ushort value, WriterHelper writer)
		{
			WriteUInt32(value, writer);
		}

		public static void WriteByte(byte value, WriterHelper writer)
		{
			WriteUInt32(value, writer);
		}

		public static void WriteSByte(sbyte value, WriterHelper writer)
		{
			WriteInt32(value, writer);
		}

		private static void WriteInt32ToBuffer(int value, byte[] buffer, int index)
		{
			buffer[index] = (byte)value;
			buffer[index + 1] = (byte)(value >> 8);
			buffer[index + 2] = (byte)(value >> 16);
			buffer[index + 3] = (byte)(value >> 24);
		}

		public static void WriteInt32(int value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed32:
				DemandSpace(4, writer);
				WriteInt32ToBuffer(value, writer.ioBuffer, writer.ioIndex);
				IncrementedAndReset(4, writer);
				break;
			case WireType.Fixed64:
			{
				DemandSpace(8, writer);
				byte[] array = writer.ioBuffer;
				int num = writer.ioIndex;
				array[num] = (byte)value;
				array[num + 1] = (byte)(value >> 8);
				array[num + 2] = (byte)(value >> 16);
				array[num + 3] = (byte)(value >> 24);
				byte[] array5 = array;
				int num5 = num + 4;
				byte[] array6 = array;
				int num6 = num + 5;
				byte b;
				array[num + 6] = (b = (array[num + 7] = 0));
				array6[num6] = (b = b);
				array5[num5] = b;
				IncrementedAndReset(8, writer);
				break;
			}
			case WireType.SignedVariant:
				WriteUInt32Variant(Zig(value), writer);
				writer.wireType = WireType.None;
				break;
			case WireType.Variant:
			{
				if (value >= 0)
				{
					WriteUInt32Variant((uint)value, writer);
					writer.wireType = WireType.None;
					break;
				}
				DemandSpace(10, writer);
				byte[] array = writer.ioBuffer;
				int num = writer.ioIndex;
				array[num] = (byte)((uint)value | 0x80u);
				array[num + 1] = (byte)((uint)(value >> 7) | 0x80u);
				array[num + 2] = (byte)((uint)(value >> 14) | 0x80u);
				array[num + 3] = (byte)((uint)(value >> 21) | 0x80u);
				array[num + 4] = (byte)((uint)(value >> 28) | 0x80u);
				byte[] array2 = array;
				int num2 = num + 5;
				byte[] array3 = array;
				int num3 = num + 6;
				byte[] array4 = array;
				int num4 = num + 7;
				byte b;
				array[num + 8] = (b = byte.MaxValue);
				array4[num4] = (b = b);
				array3[num3] = (b = b);
				array2[num2] = b;
				array[num + 9] = 1;
				IncrementedAndReset(10, writer);
				break;
			}
			default:
				throw CreateException(writer);
			}
		}

		public static void WriteDouble(double value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed32:
			{
				float value2 = (float)value;
				if (Helpers.IsInfinity(value2) && !Helpers.IsInfinity(value))
				{
					throw new OverflowException();
				}
				WriteSingle(value2, writer);
				break;
			}
			case WireType.Fixed64:
				WriteInt64(BitConverter.ToInt64(BitConverter.GetBytes(value), 0), writer);
				break;
			default:
				throw CreateException(writer);
			}
		}

		public static void WriteSingle(float value, WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			switch (writer.wireType)
			{
			case WireType.Fixed32:
				WriteInt32(BitConverter.ToInt32(BitConverter.GetBytes(value), 0), writer);
				break;
			case WireType.Fixed64:
				WriteDouble(value, writer);
				break;
			default:
				throw CreateException(writer);
			}
		}

		public static void ThrowEnumException(WriterHelper writer, object enumValue)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			string text = ((enumValue == null) ? "<null>" : (enumValue.GetType().FullName + "." + enumValue.ToString()));
			throw new Exception("No wire-value is mapped to the enum " + text + " at position " + writer.position);
		}

		internal static Exception CreateException(WriterHelper writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			return new Exception("Invalid serialization operation with wire-type " + writer.wireType.ToString() + " at position " + writer.position);
		}

		public static void WriteBoolean(bool value, WriterHelper writer)
		{
			WriteUInt32(value ? 1u : 0u, writer);
		}

		public static void SetPackedField(int fieldNumber, WriterHelper writer)
		{
			if (fieldNumber <= 0)
			{
				throw new ArgumentOutOfRangeException("fieldNumber");
			}
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			writer.packedFieldNumber = fieldNumber;
		}

	}

}
