#pragma warning disable CS0659
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Simplex;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.IO.Compression;
using Simplex.Serialization;
using System.Text.Json.Serialization;
using Simplex.Util;

namespace SimplexLambda.User
{
    [Flags]
    public enum SimplexAccessFlags : ulong
    {
                  None = 0x0000000000000000,

           GetUserData = 0x0000000000000001,
           SetUserData = 0x0000000000000002,
        UpdateUserData = 0x0000000000000004,

                 Admin = 0x8000000000000000
    }

    public class SimplexAccessToken : ISmpSerializer
    {
        static readonly Random _random = new Random();
        public static ISimplexLogger logger;

        private long _entropy = ((long)_random.Next() << 32) | _random.Next();
        [JsonIgnore]
        public long Entropy { get => _entropy; }
        private Guid _userGUID = Guid.Empty;
        [JsonIgnore]
        public Guid UserGUID { get => _userGUID; set => _userGUID = value; }
        private ulong _accessFlags;
        [JsonIgnore]
        public SimplexAccessFlags AccessFlags { get => (SimplexAccessFlags)_accessFlags; set => _accessFlags = (ulong)value; }
        private long _created;
        [JsonIgnore]
        public DateTime Created { get => DateTime.FromBinary(_created); set => _created = value.ToBinary(); }
        private string _clientId;
        [JsonIgnore]
        public string ClientID { get => _clientId; set => _clientId = value; }

        public SimplexAccessToken()
        {
            AccessFlags = SimplexAccessFlags.None;
        }

        public void Serialize(SmpSerializationStructure repo)
        {
            logger?.Debug($"access token serializing - Type: {repo.GetType()}   IsRead: {repo.IsRead}   Size: {repo.Size}");
            logger?.Debug($"    before serialization: {this}");

            repo.Int64(ref _entropy);

            Span<byte> sp = stackalloc byte[16];
            _userGUID.TryWriteBytes(sp);
            repo.Bytes(ref sp);
            _userGUID = new Guid(sp);

            repo.UInt64(ref _accessFlags);

            repo.Int64(ref _created);

            repo.String(ref _clientId);

            logger?.Debug($"done serializing - {this}");
        }

        public override bool Equals(object obj)
        {
            SimplexAccessToken sat = obj as SimplexAccessToken;
            if (sat == null)
                return false;

            logger?.Debug($"this: {this}");
            logger?.Debug($"sat: {sat}");

            return this._entropy == sat._entropy
                && this._userGUID == sat._userGUID
                && this._accessFlags == sat._accessFlags
                && this._created == sat._created;
        }

        public byte[] SerializeSignAndEncrypt(RSA rsa, Aes aes, SimplexDiagnostics diag)
        {
            var handle = diag.BeginDiag("ACCESS_TOKEN_SERIALIZATION");

            using (MemoryStream ms = new MemoryStream())
            using (MemoryStream finalMs = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(finalMs, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                this.SmpWrite(ms);

                var dataStream = new RSASignedDataStream(cs, rsa as RSACryptoServiceProvider);
                dataStream.logger = logger;
                dataStream.WriteAndSign(ms.ToArray());

                cs.FlushFinalBlock();

                diag.EndDiag(handle);
                return finalMs.ToArray();
            }
        }

        public SimplexError DecryptVerifyAndDeserialize(byte[] bytes, RSA rsa, Aes aes, SimplexDiagnostics diag, out SimplexError err)
        {
            var handle = diag.BeginDiag("ACCESS_TOKEN_DESERIALIZATION");

            using (MemoryStream ms = new MemoryStream(bytes))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                byte[] sig = new byte[rsa.SignatureSize()];
                byte[] data = new byte[this.SmpSize()];
                try
                {
                    RSASignedDataStream dataStream = new RSASignedDataStream(cs, rsa as RSACryptoServiceProvider);
                    dataStream.logger = logger;
                    if (!dataStream.ReadAndVerify(new Span<byte>(data, 0, data.Length)))
                    {
                        err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid);
                        return err;
                    }
                    logger?.Debug(new Span<byte>(data, 0, data.Length).ToHexString());
                }
                catch (CryptographicException ex)
                {
                    err = SimplexError.GetError(SimplexErrorCode.AccessTokenInvalid, ex.Message);
                    return err;
                }

                using (MemoryStream dataMs = new MemoryStream(data))
                {
                    logger?.Debug($"dataMs len: {dataMs.Length}");
                    logger?.Debug(data.AsSpan().ToHexString());
                    this.SmpRead(dataMs);
                }
            }

            diag.EndDiag(handle);

            err = SimplexError.OK;
            return err;
        }

        public override string ToString()
        {
            return $"{_entropy} {_userGUID} {AccessFlags} {_created}";
        }

        public static SimplexError FromString(string token, SimplexRequestContext context, out SimplexAccessToken accessToken, out SimplexError err)
        {
            var bytes = token.ToHexBytes();
            accessToken = new SimplexAccessToken();
            accessToken.DecryptVerifyAndDeserialize(bytes, context.RSA, context.AES, context.DiagInfo, out var decryptErr);
            err = decryptErr;
            return err;
        }
    }
}
