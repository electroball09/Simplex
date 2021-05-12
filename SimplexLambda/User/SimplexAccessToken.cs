#pragma warning disable CS0659
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Simplex;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SimplexLambda.User
{
    [Flags]
    public enum SimplexAccessFlags
    {
        None = 0,
    }

    public interface ISmpSerializer
    {
        void Serialize(SmpSerializationStructure repo);
    }

    public static class SmpSerialization
    {
        public static void SmpRead(this ISmpSerializer serializer, Stream stream)
        {
            SmpSerializationStructureRead read = new SmpSerializationStructureRead(stream);
            serializer.Serialize(read);
        }

        public static void SmpWrite(this ISmpSerializer serializer, Stream stream)
        {
            SmpSerializationStructureWrite write = new SmpSerializationStructureWrite(stream);
            serializer.Serialize(write);
        }

        public static long SmpSize(this ISmpSerializer serializer)
        {
            SmpSerializationStructureSize size = new SmpSerializationStructureSize();
            serializer.Serialize(size);
            return size.CurrentSize;
        }
    }

    public abstract class SmpSerializationStructure
    {
        protected readonly Stream _stream;

        public SmpSerializationStructure() { }
        public SmpSerializationStructure(Stream stream) : this() => _stream = stream;

        protected abstract void Int32Impl(ref int value);
        protected abstract void Int64Impl(ref long value);
        protected abstract void BytesImpl(ref Span<byte> value);
        protected abstract void ByteImpl(ref byte value);

        public void Int32(ref int value) => Int32Impl(ref value);
        public void Int64(ref long value) => Int64Impl(ref value);
        public void Bytes(ref Span<byte> value) => BytesImpl(ref value);
        public void Byte(ref byte value) => ByteImpl(ref value);
        public void Serializer<T>(ref T value) where T : ISmpSerializer => value.Serialize(this);
    }

    public class SmpSerializationStructureWrite : SmpSerializationStructure
    {
        BinaryWriter _bw;

        public SmpSerializationStructureWrite(Stream stream)
            : base(stream)
        {
            _bw = new BinaryWriter(stream);
        }

        protected override void BytesImpl(ref Span<byte> value) => _stream.Write(value);
        protected override void Int32Impl(ref int value) => _bw.Write(value);
        protected override void Int64Impl(ref long value) => _bw.Write(value);
        protected override void ByteImpl(ref byte value) => _bw.Write(value);
    }

    public class SmpSerializationStructureRead : SmpSerializationStructure
    {
        BinaryReader _br;

        public SmpSerializationStructureRead(Stream stream)
            : base(stream)
        {
            _br = new BinaryReader(stream);
        }

        protected override void BytesImpl(ref Span<byte> value) => _stream.Read(value);
        protected override void Int32Impl(ref int value) => value = _br.ReadInt32();
        protected override void Int64Impl(ref long value) => value = _br.ReadInt64();
        protected override void ByteImpl(ref byte value) => value = _br.ReadByte();
    }

    public class SmpSerializationStructureSize : SmpSerializationStructure
    {
        private long size = 0;
        public long CurrentSize => size;

        protected override void BytesImpl(ref Span<byte> value) => size += value.Length;
        protected override void Int32Impl(ref int value) => size += sizeof(int);
        protected override void Int64Impl(ref long value) => size += sizeof(long);
        protected override void ByteImpl(ref byte value) => size += sizeof(byte);
    }

    public class SimplexAccessToken : ISmpSerializer
    {
        static readonly Random _random = new Random();

        private long _entropy = ((long)_random.Next() << 32) & _random.Next();
        public long Entropy { get => _entropy; }
        private Guid _userGUID = Guid.Empty;
        public Guid UserGUID { get => _userGUID; set => _userGUID = value; }
        private int _accessFlags;
        public SimplexAccessFlags AccessFlags { get => (SimplexAccessFlags)_accessFlags; set => _accessFlags = (int)value; }
        private long _created;
        public DateTime Created { get => DateTime.FromBinary(_created); set => _created = value.ToBinary(); }

        public SimplexAccessToken()
        {
            AccessFlags = SimplexAccessFlags.None;
        }

        public void Serialize(SmpSerializationStructure repo)
        {
            repo.Int64(ref _entropy);

            Span<byte> sp = stackalloc byte[16];
            _userGUID.TryWriteBytes(sp);
            repo.Bytes(ref sp);
            _userGUID = new Guid(sp);

            repo.Int32(ref _accessFlags);

            repo.Int64(ref _created);
        }

        public override bool Equals(object obj)
        {
            SimplexAccessToken sat = obj as SimplexAccessToken;
            if (sat == null)
                return false;

            return this._entropy == sat._entropy
                && this._userGUID == sat._userGUID
                && this._accessFlags == sat._accessFlags
                && this._created == sat._created;
        }

        public byte[] SerializeSignAndEncrypt(RSA rsa, Aes aes, RequestDiagnostics diag)
        {
            var handle = diag.BeginDiag("ACCESS_TOKEN_SERIALIZATION");
            
            using (MemoryStream ms = new MemoryStream())
            using (MemoryStream finalMs = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(finalMs, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                this.SmpWrite(ms);

                ms.Position = 0;
                byte[] signature = rsa.SignData(ms, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                cs.Write(signature);
                this.SmpWrite(cs);

                cs.FlushFinalBlock();

                diag.EndDiag(handle);
                return finalMs.ToArray();
            }
        }

        public SimplexError DecryptVerifyAndDeserialize(byte[] bytes, RSA rsa, Aes aes, RequestDiagnostics diag, out SimplexError err)
        {
            var handle = diag.BeginDiag("ACCESS_TOKEN_DESERIALIZATION");

            using (MemoryStream ms = new MemoryStream(bytes))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                byte[] sig = new byte[rsa.KeySize / 8];
                cs.Read(sig, 0, sig.Length);
                byte[] data = new byte[this.SmpSize()];
                cs.Read(data, 0, data.Length);

                bool verified = rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                if (!verified)
                {
                    err = SimplexError.GetError(SimplexErrorCode.Unknown, "token verification failed!");
                    return err;
                }

                using (MemoryStream dataMs = new MemoryStream(data))
                {
                    this.SmpRead(dataMs);
                }
            }

            diag.EndDiag(handle);

            err = SimplexError.OK;
            return err;
        }
    }
}
