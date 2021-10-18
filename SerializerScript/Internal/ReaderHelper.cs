using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SerializerScript.Internal
{
    public sealed class ReaderHelper : IDisposable
    {
        internal const int TO_EOF = -1;

        private const long Int64Msb = long.MinValue;

        private const int Int32Msb = int.MinValue;

        private Stream source;

        private byte[] ioBuffer;

        private int fieldNumber;

        private int depth;

        private int dataRemaining;

        private int ioIndex;

        private int position;

        private int available;

        private int blockEnd;

        private WireType wireType;

        private bool isFixedLength;

        private bool internStrings;

        private uint trapCount;

        private Dictionary<string, string> stringInterner;

        private static readonly UTF8Encoding encoding = new UTF8Encoding();

        private static readonly byte[] EmptyBlob = new byte[0];

        [ThreadStatic]
        private static ReaderHelper lastReader;

        public int FieldNumber {
            get
            {
                return fieldNumber;
            }
        }

        public WireType WireType {
            get
            {
                return wireType;
            }
        }

        public bool InternStrings
        {
            get
            {
                return internStrings;
            }
            set
            {
                internStrings = value;
            }
        }

        public int Position { get { return position; } }

        public ReaderHelper(Stream source, int length = -1)
        {
            Init(this, source, length);
        }

        private static void Init(ReaderHelper reader, Stream source, int length = -1)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (!source.CanRead)
            {
                throw new ArgumentException("Cannot read from stream", "source");
            }
            reader.source = source;
            reader.ioBuffer = BufferPool.GetBuffer();
            reader.dataRemaining = ((reader.isFixedLength = length >= 0) ? length : 0);
            reader.position = (reader.available = (reader.depth = (reader.fieldNumber = (reader.ioIndex = 0))));
            reader.blockEnd = int.MaxValue;
            reader.internStrings = true;
            reader.wireType = WireType.None;
            reader.trapCount = 1u;
        }

        public void Dispose()
        {
            source = null;
            BufferPool.ReleaseBufferToPool(ref ioBuffer);
            if (stringInterner != null)
            {
                stringInterner.Clear();
            }
        }

        internal int TryReadUInt32VariantWithoutMoving(bool trimNegative, out uint value)
        {
            if (available < 10)
            {
                Ensure(10, strict: false);
            }
            if (available == 0)
            {
                value = 0u;
                return 0;
            }
            int num = ioIndex;
            value = ioBuffer[num++];
            if ((value & 0x80) == 0)
            {
                return 1;
            }
            value &= 127u;
            if (available == 1)
            {
                throw EoF(this);
            }
            uint num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 7;
            if ((num2 & 0x80) == 0)
            {
                return 2;
            }
            if (available == 2)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 14;
            if ((num2 & 0x80) == 0)
            {
                return 3;
            }
            if (available == 3)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 21;
            if ((num2 & 0x80) == 0)
            {
                return 4;
            }
            if (available == 4)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num];
            value |= num2 << 28;
            if ((num2 & 0xF0) == 0)
            {
                return 5;
            }
            if (trimNegative && (num2 & 0xF0) == 240 && available >= 10 && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == byte.MaxValue && ioBuffer[++num] == 1)
            {
                return 10;
            }
            throw AddErrorData(new OverflowException(), this);
        }

        private uint ReadUInt32Variant(bool trimNegative)
        {
            uint value;
            int num = TryReadUInt32VariantWithoutMoving(trimNegative, out value);
            if (num > 0)
            {
                ioIndex += num;
                available -= num;
                position += num;
                return value;
            }
            throw EoF(this);
        }

        private bool TryReadUInt32Variant(out uint value)
        {
            int num = TryReadUInt32VariantWithoutMoving( false, out value);
            if (num > 0)
            {
                ioIndex += num;
                available -= num;
                position += num;
                return true;
            }
            return false;
        }

        public uint ReadUInt32()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return ReadUInt32Variant(trimNegative: false);
                case WireType.Fixed32:
                    if (available < 4)
                    {
                        Ensure(4, strict: true);
                    }
                    position += 4;
                    available -= 4;
                    return (uint)(ioBuffer[ioIndex++] | (ioBuffer[ioIndex++] << 8) | (ioBuffer[ioIndex++] << 16) | (ioBuffer[ioIndex++] << 24));
                case WireType.Fixed64:
                    {
                        ulong num = ReadUInt64();
                        return checked((uint)num);
                    }
                default:
                    throw CreateWireTypeException();
            }
        }

        internal void Ensure(int count, bool strict)
        {
            Helpers.DebugAssert(available <= count, "Asking for data without checking first");
            if (count > ioBuffer.Length)
            {
                BufferPool.ResizeAndFlushLeft(ref ioBuffer, count, ioIndex, available);
                ioIndex = 0;
            }
            else if (ioIndex + count >= ioBuffer.Length)
            {
                Helpers.BlockCopy(ioBuffer, ioIndex, ioBuffer, 0, available);
                ioIndex = 0;
            }
            count -= available;
            int num = ioIndex + available;
            int num2 = ioBuffer.Length - num;
            if (isFixedLength && dataRemaining < num2)
            {
                num2 = dataRemaining;
            }
            int num3;
            while (count > 0 && num2 > 0 && (num3 = source.Read(ioBuffer, num, num2)) > 0)
            {
                available += num3;
                count -= num3;
                num2 -= num3;
                num += num3;
                if (isFixedLength)
                {
                    dataRemaining -= num3;
                }
            }
            if (strict && count > 0)
            {
                throw EoF(this);
            }
        }

        public short ReadInt16()
        {
            return checked((short)ReadInt32());
        }

        public ushort ReadUInt16()
        {
            return checked((ushort)ReadUInt32());
        }

        public byte ReadByte()
        {
            return checked((byte)ReadUInt32());
        }

        public sbyte ReadSByte()
        {
            return checked((sbyte)ReadInt32());
        }

        public int ReadInt32()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return (int)ReadUInt32Variant(trimNegative: true);
                case WireType.Fixed32:
                    if (available < 4)
                    {
                        Ensure(4, strict: true);
                    }
                    position += 4;
                    available -= 4;
                    return ioBuffer[ioIndex++] | (ioBuffer[ioIndex++] << 8) | (ioBuffer[ioIndex++] << 16) | (ioBuffer[ioIndex++] << 24);
                case WireType.Fixed64:
                    {
                        long num = ReadInt64();
                        return checked((int)num);
                    }
                case WireType.SignedVariant:
                    return Zag(ReadUInt32Variant(trimNegative: true));
                default:
                    throw CreateWireTypeException();
            }
        }

        private static int Zag(uint ziggedValue)
        {
            return (int)(0 - (ziggedValue & 1)) ^ (((int)ziggedValue >> 1) & 0x7FFFFFFF);
        }

        private static long Zag(ulong ziggedValue)
        {
            return (long)(0L - (ziggedValue & 1)) ^ (((long)ziggedValue >> 1) & 0x7FFFFFFFFFFFFFFFL);
        }

        public long ReadInt64()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return (long)ReadUInt64Variant();
                case WireType.Fixed32:
                    return ReadInt32();
                case WireType.Fixed64:
                    if (available < 8)
                    {
                        Ensure(8, strict: true);
                    }
                    position += 8;
                    available -= 8;
                    return (long)(ioBuffer[ioIndex++] | ((ulong)ioBuffer[ioIndex++] << 8) | ((ulong)ioBuffer[ioIndex++] << 16) | ((ulong)ioBuffer[ioIndex++] << 24) | ((ulong)ioBuffer[ioIndex++] << 32) | ((ulong)ioBuffer[ioIndex++] << 40) | ((ulong)ioBuffer[ioIndex++] << 48) | ((ulong)ioBuffer[ioIndex++] << 56));
                case WireType.SignedVariant:
                    return Zag(ReadUInt64Variant());
                default:
                    throw CreateWireTypeException();
            }
        }

        private int TryReadUInt64VariantWithoutMoving(out ulong value)
        {
            if (available < 10)
            {
                Ensure(10, strict: false);
            }
            if (available == 0)
            {
                value = 0uL;
                return 0;
            }
            int num = ioIndex;
            value = ioBuffer[num++];
            if ((value & 0x80) == 0)
            {
                return 1;
            }
            value &= 127uL;
            if (available == 1)
            {
                throw EoF(this);
            }
            ulong num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 7;
            if ((num2 & 0x80) == 0)
            {
                return 2;
            }
            if (available == 2)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 14;
            if ((num2 & 0x80) == 0)
            {
                return 3;
            }
            if (available == 3)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 21;
            if ((num2 & 0x80) == 0)
            {
                return 4;
            }
            if (available == 4)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 28;
            if ((num2 & 0x80) == 0)
            {
                return 5;
            }
            if (available == 5)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 35;
            if ((num2 & 0x80) == 0)
            {
                return 6;
            }
            if (available == 6)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 42;
            if ((num2 & 0x80) == 0)
            {
                return 7;
            }
            if (available == 7)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 49;
            if ((num2 & 0x80) == 0)
            {
                return 8;
            }
            if (available == 8)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num++];
            value |= (num2 & 0x7F) << 56;
            if ((num2 & 0x80) == 0)
            {
                return 9;
            }
            if (available == 9)
            {
                throw EoF(this);
            }
            num2 = ioBuffer[num];
            value |= num2 << 63;
            if ((num2 & 0xFFFFFFFFFFFFFFFEuL) != 0)
            {
                throw AddErrorData(new OverflowException(), this);
            }
            return 10;
        }

        private ulong ReadUInt64Variant()
        {
            ulong value;
            int num = TryReadUInt64VariantWithoutMoving(out value);
            if (num > 0)
            {
                ioIndex += num;
                available -= num;
                position += num;
                return value;
            }
            throw EoF(this);
        }

        private string Intern(string value)
        {
            if (value == null)
            {
                return null;
            }
            if (value.Length == 0)
            {
                return "";
            }
            string value2;
            if (stringInterner == null)
            {
                stringInterner = new Dictionary<string, string>();
                stringInterner.Add(value, value);
            }
            else if (stringInterner.TryGetValue(value, out value2))
            {
                value = value2;
            }
            else
            {
                stringInterner.Add(value, value);
            }
            return value;
        }

        public string ReadString()
        {
            if (wireType == WireType.String)
            {
                int num = (int)ReadUInt32Variant(trimNegative: false);
                if (num == 0)
                {
                    return "";
                }
                if (available < num)
                {
                    Ensure(num, strict: true);
                }
                string text = encoding.GetString(ioBuffer, ioIndex, num);
                if (internStrings)
                {
                    text = Intern(text);
                }
                available -= num;
                position += num;
                ioIndex += num;
                return text;
            }
            throw CreateWireTypeException();
        }

        public void ThrowEnumException(Type type, int value)
        {
            string text = ((type == null) ? "<null>" : type.FullName);
            throw AddErrorData(new Exception("No " + text + " enum is mapped to the wire-value " + value), this);
        }

        private Exception CreateWireTypeException()
        {
            return CreateException("Invalid wire-type; this usually means you have over-written a file without truncating or setting the length; see http://stackoverflow.com/q/2152978/23354");
        }

        private Exception CreateException(string message)
        {
            return AddErrorData(new Exception(message), this);
        }

        public double ReadDouble()
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    return ReadSingle();
                case WireType.Fixed64:
                    {
                        long value = ReadInt64();
                        return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
                    }
                default:
                    throw CreateWireTypeException();
            }
        }

        public static void EndSubItem(SubItemToken token, ReaderHelper reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            int value = token.value;
            WireType wireType = reader.wireType;
            if (wireType == WireType.EndGroup)
            {
                if (value >= 0)
                {
                    throw AddErrorData(new ArgumentException("token"), reader);
                }
                if (-value != reader.fieldNumber)
                {
                    throw reader.CreateException("Wrong group was ended");
                }
                reader.wireType = WireType.None;
                reader.depth--;
            }
            else
            {
                if (value < reader.position)
                {
                    throw reader.CreateException("Sub-message not read entirely");
                }
                if (reader.blockEnd != reader.position && reader.blockEnd != int.MaxValue)
                {
                    throw reader.CreateException("Sub-message not read correctly");
                }
                reader.blockEnd = value;
                reader.depth--;
            }
        }

        public static SubItemToken StartSubItem(ReaderHelper reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            switch (reader.wireType)
            {
                case WireType.StartGroup:
                    reader.wireType = WireType.None;
                    reader.depth++;
                    return new SubItemToken(-reader.fieldNumber);
                case WireType.String:
                    {
                        int num = (int)reader.ReadUInt32Variant(trimNegative: false);
                        if (num < 0)
                        {
                            throw AddErrorData(new InvalidOperationException(), reader);
                        }
                        int value = reader.blockEnd;
                        reader.blockEnd = reader.position + num;
                        reader.depth++;
                        return new SubItemToken(value);
                    }
                default:
                    throw reader.CreateWireTypeException();
            }
        }

        public int ReadFieldHeader()
        {
            if (blockEnd <= position || wireType == WireType.EndGroup)
            {
                return 0;
            }
            uint value;
            if (TryReadUInt32Variant(out value))
            {
                wireType = (WireType)((int)value & 7);
                fieldNumber = (int)(value >> 3);
                if (fieldNumber < 1)
                {
                    throw new Exception("Invalid field in source data: " + fieldNumber);
                }
            }
            else
            {
                wireType = WireType.None;
                fieldNumber = 0;
            }
            if (wireType == WireType.EndGroup)
            {
                if (depth > 0)
                {
                    return 0;
                }
                throw new Exception("Unexpected end-group in source data; this usually means the source data is corrupt");
            }
            return fieldNumber;
        }

        public bool TryReadFieldHeader(int field)
        {
            if (blockEnd <= position || this.wireType == WireType.EndGroup)
            {
                return false;
            }
            uint value;
            int num = TryReadUInt32VariantWithoutMoving(false, out value);
            WireType wireType;
            if (num > 0 && (int)value >> 3 == field && (wireType = (WireType)((int)value & 7)) != WireType.EndGroup)
            {
                this.wireType = wireType;
                fieldNumber = field;
                position += num;
                ioIndex += num;
                available -= num;
                return true;
            }
            return false;
        }

        public void Hint(WireType wireType)
        {
            if (this.wireType != wireType && (wireType & (WireType)7) == this.wireType)
            {
                this.wireType = wireType;
            }
        }

        public void Assert(WireType wireType)
        {
            if (this.wireType != wireType)
            {
                if ((wireType & (WireType)7) != this.wireType)
                {
                    throw CreateWireTypeException();
                }
                this.wireType = wireType;
            }
        }

        public void SkipField()
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    if (available < 4)
                    {
                        Ensure(4, strict: true);
                    }
                    available -= 4;
                    ioIndex += 4;
                    position += 4;
                    break;
                case WireType.Fixed64:
                    if (available < 8)
                    {
                        Ensure(8, strict: true);
                    }
                    available -= 8;
                    ioIndex += 8;
                    position += 8;
                    break;
                case WireType.String:
                    {
                        int num2 = (int)ReadUInt32Variant(trimNegative: false);
                        if (num2 <= available)
                        {
                            available -= num2;
                            ioIndex += num2;
                            position += num2;
                            break;
                        }
                        position += num2;
                        num2 -= available;
                        ioIndex = (available = 0);
                        if (isFixedLength)
                        {
                            if (num2 > dataRemaining)
                            {
                                throw EoF(this);
                            }
                            dataRemaining -= num2;
                        }
                        Seek(source, num2, ioBuffer);
                        break;
                    }
                case WireType.Variant:
                case WireType.SignedVariant:
                    ReadUInt64Variant();
                    break;
                case WireType.StartGroup:
                    {
                        int num = fieldNumber;
                        depth++;
                        while (ReadFieldHeader() > 0)
                        {
                            SkipField();
                        }
                        depth--;
                        if (wireType == WireType.EndGroup && fieldNumber == num)
                        {
                            wireType = WireType.None;
                            break;
                        }
                        throw CreateWireTypeException();
                    }
                default:
                    throw CreateWireTypeException();
            }
        }

        public ulong ReadUInt64()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return ReadUInt64Variant();
                case WireType.Fixed32:
                    return ReadUInt32();
                case WireType.Fixed64:
                    if (available < 8)
                    {
                        Ensure(8, strict: true);
                    }
                    position += 8;
                    available -= 8;
                    return ioBuffer[ioIndex++] | ((ulong)ioBuffer[ioIndex++] << 8) | ((ulong)ioBuffer[ioIndex++] << 16) | ((ulong)ioBuffer[ioIndex++] << 24) | ((ulong)ioBuffer[ioIndex++] << 32) | ((ulong)ioBuffer[ioIndex++] << 40) | ((ulong)ioBuffer[ioIndex++] << 48) | ((ulong)ioBuffer[ioIndex++] << 56);
                default:
                    throw CreateWireTypeException();
            }
        }

        public float ReadSingle()
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    {
                        int value = ReadInt32();
                        return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
                    }
                case WireType.Fixed64:
                    {
                        double num = ReadDouble();
                        float num2 = (float)num;
                        if (Helpers.IsInfinity(num2) && !Helpers.IsInfinity(num))
                        {
                            throw AddErrorData(new OverflowException(), this);
                        }
                        return num2;
                    }
                default:
                    throw CreateWireTypeException();
            }
        }

        public bool ReadBoolean()
        {
            bool ret = false;
            switch (ReadUInt32())
            {
                case 0u:
                    ret = false;
                    break;
                case 1u:
                    ret = true;
                    break;
                default:
                    throw CreateException("Unexpected boolean value");
            };
            return ret;
        }

        public static byte[] AppendBytes(byte[] value, ReaderHelper reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            WireType wireType = reader.wireType;
            if (wireType == WireType.String)
            {
                int num = (int)reader.ReadUInt32Variant(trimNegative: false);
                reader.wireType = WireType.None;
                if (num == 0)
                {
                    return (value == null) ? EmptyBlob : value;
                }
                int num2;
                if (value == null || value.Length == 0)
                {
                    num2 = 0;
                    value = new byte[num];
                }
                else
                {
                    num2 = value.Length;
                    byte[] array = new byte[value.Length + num];
                    Helpers.BlockCopy(value, 0, array, 0, value.Length);
                    value = array;
                }
                reader.position += num;
                while (num > reader.available)
                {
                    if (reader.available > 0)
                    {
                        Helpers.BlockCopy(reader.ioBuffer, reader.ioIndex, value, num2, reader.available);
                        num -= reader.available;
                        num2 += reader.available;
                        reader.ioIndex = (reader.available = 0);
                    }
                    int num3 = ((num > reader.ioBuffer.Length) ? reader.ioBuffer.Length : num);
                    if (num3 > 0)
                    {
                        reader.Ensure(num3, strict: true);
                    }
                }
                if (num > 0)
                {
                    Helpers.BlockCopy(reader.ioBuffer, reader.ioIndex, value, num2, num);
                    reader.ioIndex += num;
                    reader.available -= num;
                }
                return value;
            }
            throw reader.CreateWireTypeException();
        }

        private static int ReadByteOrThrow(Stream source)
        {
            int num = source.ReadByte();
            if (num < 0)
            {
                throw EoF(null);
            }
            return num;
        }

        public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber)
        {
            int bytesRead;
            return ReadLengthPrefix(source, expectHeader, style, out fieldNumber, out bytesRead);
        }

        public static int DirectReadLittleEndianInt32(Stream source)
        {
            return ReadByteOrThrow(source) | (ReadByteOrThrow(source) << 8) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 24);
        }

        public static int DirectReadBigEndianInt32(Stream source)
        {
            return (ReadByteOrThrow(source) << 24) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 8) | ReadByteOrThrow(source);
        }

        public static int DirectReadVarintInt32(Stream source)
        {
            uint value;
            int num = TryReadUInt32Variant(source, out value);
            if (num <= 0)
            {
                throw EoF(null);
            }
            return (int)value;
        }

        public static void DirectReadBytes(Stream source, byte[] buffer, int offset, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int num;
            while (count > 0 && (num = source.Read(buffer, offset, count)) > 0)
            {
                count -= num;
                offset += num;
            }
            if (count > 0)
            {
                throw EoF(null);
            }
        }

        public static byte[] DirectReadBytes(Stream source, int count)
        {
            byte[] array = new byte[count];
            DirectReadBytes(source, array, 0, count);
            return array;
        }

        public static string DirectReadString(Stream source, int length)
        {
            byte[] array = new byte[length];
            DirectReadBytes(source, array, 0, length);
            return Encoding.UTF8.GetString(array, 0, length);
        }

        public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber, out int bytesRead)
        {
            fieldNumber = 0;
            switch (style)
            {
                case PrefixStyle.None:
                    bytesRead = 0;
                    return int.MaxValue;
                case PrefixStyle.Base128:
                    {
                        bytesRead = 0;
                        uint value;
                        int num2;
                        if (expectHeader)
                        {
                            num2 = TryReadUInt32Variant(source, out value);
                            bytesRead += num2;
                            if (num2 > 0)
                            {
                                if ((value & 7) != 2)
                                {
                                    throw new InvalidOperationException();
                                }
                                fieldNumber = (int)(value >> 3);
                                num2 = TryReadUInt32Variant(source, out value);
                                bytesRead += num2;
                                if (bytesRead == 0)
                                {
                                    throw EoF(null);
                                }
                                return (int)value;
                            }
                            bytesRead = 0;
                            return -1;
                        }
                        num2 = TryReadUInt32Variant(source, out value);
                        bytesRead += num2;
                        return (bytesRead < 0) ? (-1) : ((int)value);
                    }
                case PrefixStyle.Fixed32:
                    {
                        int num = source.ReadByte();
                        if (num < 0)
                        {
                            bytesRead = 0;
                            return -1;
                        }
                        bytesRead = 4;
                        return num | (ReadByteOrThrow(source) << 8) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 24);
                    }
                case PrefixStyle.Fixed32BigEndian:
                    {
                        int num = source.ReadByte();
                        if (num < 0)
                        {
                            bytesRead = 0;
                            return -1;
                        }
                        bytesRead = 4;
                        return (num << 24) | (ReadByteOrThrow(source) << 16) | (ReadByteOrThrow(source) << 8) | ReadByteOrThrow(source);
                    }
                default:
                    throw new ArgumentOutOfRangeException("style");
            }
        }

        private static int TryReadUInt32Variant(Stream source, out uint value)
        {
            value = 0u;
            int num = source.ReadByte();
            if (num < 0)
            {
                return 0;
            }
            value = (uint)num;
            if ((value & 0x80) == 0)
            {
                return 1;
            }
            value &= 127u;
            num = source.ReadByte();
            if (num < 0)
            {
                throw EoF(null);
            }
            value |= (uint)((num & 0x7F) << 7);
            if ((num & 0x80) == 0)
            {
                return 2;
            }
            num = source.ReadByte();
            if (num < 0)
            {
                throw EoF(null);
            }
            value |= (uint)((num & 0x7F) << 14);
            if ((num & 0x80) == 0)
            {
                return 3;
            }
            num = source.ReadByte();
            if (num < 0)
            {
                throw EoF(null);
            }
            value |= (uint)((num & 0x7F) << 21);
            if ((num & 0x80) == 0)
            {
                return 4;
            }
            num = source.ReadByte();
            if (num < 0)
            {
                throw EoF(null);
            }
            value |= (uint)(num << 28);
            if ((num & 0xF0) == 0)
            {
                return 5;
            }
            throw new OverflowException();
        }

        internal static void Seek(Stream source, int count, byte[] buffer)
        {
            int num;
            if (source.CanSeek)
            {
                source.Seek(count, SeekOrigin.Current);
                count = 0;
            }
            else if (buffer != null)
            {
                while (count > buffer.Length && (num = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    count -= num;
                }
                while (count > 0 && (num = source.Read(buffer, 0, count)) > 0)
                {
                    count -= num;
                }
            }
            else
            {
                buffer = BufferPool.GetBuffer();
                try
                {
                    while (count > buffer.Length && (num = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        count -= num;
                    }
                    while (count > 0 && (num = source.Read(buffer, 0, count)) > 0)
                    {
                        count -= num;
                    }
                }
                finally
                {
                    BufferPool.ReleaseBufferToPool(ref buffer);
                }
            }
            if (count > 0)
            {
                throw EoF(null);
            }
        }

        internal static Exception AddErrorData(Exception exception, ReaderHelper source)
        {
            if (exception != null && source != null && !exception.Data.Contains("protoSource"))
            {
                exception.Data.Add("protoSource", "tag={" + source.fieldNumber.ToString() + "}; wire-type={" + source.wireType.ToString() + "}; offset={" + source.position.ToString() + "}; depth={" + source.depth.ToString() + "}");
            }
            return exception;
        }

        private static Exception EoF(ReaderHelper source)
        {
            return AddErrorData(new EndOfStreamException(), source);
        }

        public static bool HasSubValue(WireType wireType, ReaderHelper source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (source.blockEnd <= source.position || wireType == WireType.EndGroup)
            {
                return false;
            }
            source.wireType = wireType;
            return true;
        }

        internal static ReaderHelper Create(Stream source, int len)
        {
            ReaderHelper recycled = GetRecycled();
            if (recycled == null)
            {
                return new ReaderHelper(source, len);
            }
            Init(recycled, source, len);
            return recycled;
        }

        private static ReaderHelper GetRecycled()
        {
            ReaderHelper result = lastReader;
            lastReader = null;
            return result;
        }

        internal static void Recycle(ReaderHelper reader)
        {
            if (reader != null)
            {
                reader.Dispose();
                lastReader = reader;
            }
        }
    }
}
