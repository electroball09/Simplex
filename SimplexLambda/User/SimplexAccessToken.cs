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
using Simplex.Protocol;

namespace SimplexLambda.User
{

    public class SimplexAccessPermissions : ISmpSerializer
    {
        [Flags]
        public enum AccessFlags : ulong
        {

            None = 0x0,

            DataOp_Get = 0x1,
            DataOp_Set = 0x2,
            DataOp_Update = 0x4,

            //shortcuts
            DataOp_All = 0x7,
        }

        private ulong userData_PrivateSelf;
        public AccessFlags UserData_PrivateSelf { get => (AccessFlags)userData_PrivateSelf; set => userData_PrivateSelf = (ulong)value; }
        private ulong userData_PublicSelf;
        public AccessFlags UserData_PublicSelf { get => (AccessFlags)userData_PublicSelf; set => userData_PublicSelf = (ulong)value; }
        private ulong userData_PrivateNonSelf;
        public AccessFlags UserData_PrivateNonSelf { get => (AccessFlags)userData_PrivateNonSelf; set => userData_PrivateNonSelf = (ulong)value; }
        private ulong userData_PublicNonSelf;
        public AccessFlags UserData_PublicNonSelf { get => (AccessFlags)userData_PublicNonSelf; set => userData_PublicNonSelf = (ulong)value; }

        public void Serialize(SmpSerializationStructure repo)
        {
            repo.UInt64(ref userData_PrivateSelf);
            repo.UInt64(ref userData_PublicSelf);
            repo.UInt64(ref userData_PrivateNonSelf);
            repo.UInt64(ref userData_PublicNonSelf);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SimplexAccessPermissions perm))
                return false;

            return perm.userData_PrivateSelf == userData_PrivateSelf
                && perm.userData_PublicSelf == userData_PublicSelf
                && perm.userData_PrivateNonSelf == userData_PrivateNonSelf
                && perm.userData_PublicNonSelf == userData_PublicNonSelf;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode(); //pls stop crying at me
        }

        public static bool operator ==(SimplexAccessPermissions a, SimplexAccessPermissions b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(SimplexAccessPermissions a, SimplexAccessPermissions b)
        {
            return !(a == b);
        }
    }

    public class SimplexAccessToken : ISmpSerializer
    {
        static readonly Random _random = new Random();
        public static ISimplexLogger logger;

#pragma warning disable CS0675
        private long _entropy = ((long)_random.Next() << 32) | _random.Next();
#pragma warning restore CS0675
        [JsonIgnore]
        public long Entropy { get => _entropy; }
        private Guid _userGUID = Guid.Empty;
        [JsonIgnore]
        public Guid UserGUID { get => _userGUID; set => _userGUID = value; }
        private SimplexAccessPermissions _permissions = new SimplexAccessPermissions();
        [JsonIgnore]
        public SimplexAccessPermissions Permissions { get => _permissions; set => _permissions = value; }
        private long _created;
        [JsonIgnore]
        public DateTime CreatedUTC { get => DateTime.FromBinary(_created); set => _created = value.ToBinary(); }
        private long _expires;
        [JsonIgnore]
        public DateTime ExpiresUTC { get => DateTime.FromBinary(_expires); set => _expires = value.ToBinary(); }
        private StringWithLength _clientId = "";
        [JsonIgnore]
        public string ClientID { get => _clientId; set => _clientId = value; }
        private StringWithLength _authAccountID = "";
        [JsonIgnore]
        public string AuthAccountID { get => _authAccountID; set => _authAccountID = value; }
        private AuthServiceIdentifier _serviceIdentifier = new AuthServiceIdentifier();
        [JsonIgnore]
        public AuthServiceIdentifier ServiceIdentifier { get => _serviceIdentifier; set => _serviceIdentifier = value; }

        public void Serialize(SmpSerializationStructure repo)
        {
            logger?.Debug($"access token serializing - Type: {repo.GetType()}   IsRead: {repo.IsRead}   Size: {repo.Size}");
            logger?.Debug($"    before serialization: {this}");

            repo.Int64(ref _entropy);

            Span<byte> sp = stackalloc byte[16];
            _userGUID.TryWriteBytes(sp);
            repo.Bytes(ref sp);
            _userGUID = new Guid(sp);

            repo.Serializer(ref _permissions);
            repo.Int64(ref _created);
            repo.Int64(ref _expires);
            repo.Serializer(ref _clientId);
            repo.Serializer(ref _authAccountID);
            repo.Serializer(ref _serviceIdentifier);

            logger?.Debug($"done serializing - {this} - {repo.Size}");
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SimplexAccessToken token))
                return false;

            return this._entropy == token._entropy
                && this._userGUID == token._userGUID
                && this._permissions == token._permissions
                && this._created == token._created
                && this._clientId == token._clientId
                && this._authAccountID == token._authAccountID;
        }

        public byte[] SerializeSignAndEncrypt(RSA rsa, Aes aes, SimplexDiagnostics diag)
        {
            var handle = diag.BeginDiag("ACCESS_TOKEN_SERIALIZATION");

            using (MemoryStream ms = new MemoryStream())
            using (MemoryStream finalMs = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(finalMs, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                BinaryWriter bw = new BinaryWriter(finalMs);
                bw.Write(this.SmpWrite(ms));

                var dataStream = new RSASignedDataStream(cs, rsa as RSACryptoServiceProvider)
                {
                    logger = logger
                };
                var b = ms.ToArray();
                dataStream.WriteAndSign(b);

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
                BinaryReader br = new BinaryReader(ms);
                long size = br.ReadInt64();

                byte[] sig = new byte[rsa.SignatureSize()];
                byte[] data = new byte[size];
                try
                {
                    RSASignedDataStream dataStream = new RSASignedDataStream(cs, rsa as RSACryptoServiceProvider);
                    dataStream.logger = logger;
                    if (!dataStream.ReadAndVerify(data.AsSpan()))
                    {
                        logger?.Error("couldn't verify token");
                        err = SimplexErrorCode.AccessTokenInvalid;
                        return err;
                    }
                    logger?.Debug(new Span<byte>(data, 0, data.Length).ToHexString());
                }
                catch (Exception ex)
                {
                    err = SimplexError.Custom(SimplexErrorCode.AccessTokenInvalid, ex.Message);
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

            err = SimplexErrorCode.OK;
            return err;
        }

        public SimplexError ValidateAccessToken(SimplexRequestContext context, out SimplexError err)
        {
            var handle = context.DiagInfo.BeginDiag("VALIDATE_ACCESS_TOKEN");

            SimplexError End(SimplexError inErr, out SimplexError outErr)
            {
                outErr = inErr;
                context.DiagInfo.EndDiag(handle);
                return outErr;
            }

            if (DateTime.UtcNow > ExpiresUTC)
                return End(SimplexErrorCode.AccessTokenExpired, out err);

            if (ClientID != context.Request.ClientID)
                return End(SimplexErrorCode.AccessTokenInvalid, out err);

            return End(SimplexErrorCode.OK, out err);
        }

        public override string ToString()
        {
            return $"{_entropy} {_userGUID} {Permissions} {_created} {_clientId} {_authAccountID}";
        }

        public static SimplexError FromString(string token, SimplexRequestContext context, out SimplexAccessToken accessToken, out SimplexError err)
        {
            if (string.IsNullOrEmpty(token))
            {
                context.Log.Warn("null token???");
                accessToken = null;
                err = SimplexErrorCode.AccessTokenInvalid;
            }
            else
            {
                var bytes = token.ToHexBytes();
                accessToken = new SimplexAccessToken();
                accessToken.DecryptVerifyAndDeserialize(bytes, context.RSA, context.AES, context.DiagInfo, out err);
            }
            return err;
        }
    }
}
