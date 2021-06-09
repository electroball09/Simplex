using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Buffers.Binary;

namespace Simplex.Serialization
{
    public interface ISmpSerializer
    {
        void Serialize(SmpSerializationStructure repo);
    }

    public static class SmpSerialization
    {
        public static long SmpRead(this ISmpSerializer serializer, Stream stream)
        {
            SmpSerializationStructureRead read = new SmpSerializationStructureRead(stream);
            serializer.Serialize(read);
            return read.Size;
        }

        public static long SmpWrite(this ISmpSerializer serializer, Stream stream)
        {
            SmpSerializationStructureWrite write = new SmpSerializationStructureWrite(stream);
            serializer.Serialize(write);
            return write.Size;
        }

        public static long SmpSize(this ISmpSerializer serializer)
        {
            SmpSerializationStructureSize size = new SmpSerializationStructureSize();
            serializer.Serialize(size);
            return size.Size;
        }
    }



    public abstract class SmpSerializationStructure
    {
        protected readonly Stream _stream;

        public abstract bool IsRead { get; }
        public abstract int Size { get; }

        public SmpSerializationStructure() { }
        public SmpSerializationStructure(Stream stream) : this() => _stream = stream;

        protected abstract void Int32Impl(ref int value);
        protected abstract void Int64Impl(ref long value);
        protected abstract void UInt64Impl(ref ulong value);
        protected abstract void BytesImpl(ref Span<byte> value);
        protected abstract void BytesImpl(ref byte[] value);
        protected abstract void ByteImpl(ref byte value);
        protected abstract void StringImpl(ref string value);

        public void Int32(ref int value) => Int32Impl(ref value);
        public void Int64(ref long value) => Int64Impl(ref value);
        public void UInt64(ref ulong value) => UInt64Impl(ref value);
        public void Bytes(ref Span<byte> value) => BytesImpl(ref value);
        public void Bytes(ref byte[] value) => BytesImpl(ref value);
        public void Byte(ref byte value) => ByteImpl(ref value);
        public void String(ref string value) => StringImpl(ref value);
        public void Serializer<T>(ref T value) where T : ISmpSerializer => value.Serialize(this);
    }

    public class SmpSerializationStructureWrite : SmpSerializationStructure
    {
        public override bool IsRead => false;
        public override int Size => (int)(_stream.Position - _origStreamPosition);

        private long _origStreamPosition;

        BinaryWriter _bw;

        public SmpSerializationStructureWrite(Stream stream)
            : base(stream)
        {
            _origStreamPosition = stream.Position;
            _bw = new BinaryWriter(stream, Encoding.UTF8);
        }

        protected override void BytesImpl(ref Span<byte> value) => _stream.Write(value);
        protected override void BytesImpl(ref byte[] value) => _stream.Write(value);
        protected override void Int32Impl(ref int value) => _bw.Write(value);
        protected override void Int64Impl(ref long value) => _bw.Write(value);
        protected override void UInt64Impl(ref ulong value) => _bw.Write(value);
        protected override void ByteImpl(ref byte value) => _bw.Write(value);
        protected override void StringImpl(ref string value) => _bw.Write(value);
    }

    public class SmpSerializationStructureRead : SmpSerializationStructure
    {
        public override bool IsRead => true;
        public override int Size => (int)(_stream.Length - _origStreamPosition);

        private long _origStreamPosition;


        BinaryReader _br;

        public SmpSerializationStructureRead(Stream stream)
            : base(stream)
        {
            _origStreamPosition = stream.Position;
            _br = new BinaryReader(stream, Encoding.UTF8);
        }

        protected override void BytesImpl(ref Span<byte> value) => _stream.Read(value);
        protected override void BytesImpl(ref byte[] value) => _stream.Read(value);
        protected override void Int32Impl(ref int value) => value = _br.ReadInt32();
        protected override void Int64Impl(ref long value) => value = _br.ReadInt64();
        protected override void UInt64Impl(ref ulong value) => value = _br.ReadUInt64();
        protected override void ByteImpl(ref byte value) => value = _br.ReadByte();
        protected override void StringImpl(ref string value) => value = _br.ReadString();
    }

    public class SmpSerializationStructureSize : SmpSerializationStructure
    {
        public override bool IsRead => false;
        public override int Size => (int)size;

        private long size = 0;

        protected override void BytesImpl(ref Span<byte> value) => size += value.Length;
        protected override void BytesImpl(ref byte[] value) => size += value.Length;
        protected override void Int32Impl(ref int value) => size += sizeof(int);
        protected override void Int64Impl(ref long value) => size += sizeof(long);
        protected override void UInt64Impl(ref ulong value) => size += sizeof(ulong);
        protected override void ByteImpl(ref byte value) => size += sizeof(byte);
        protected override void StringImpl(ref string value) => size += string.IsNullOrEmpty(value) ? 1 : Encoding.UTF8.GetByteCount(value);
    }
}
