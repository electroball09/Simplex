using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Simplex;
using System.Runtime.CompilerServices;

namespace Simplex.Util
{
    public static class RSASignatureMaxDataSizes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SignatureMaxDataSize(this RSA rsa) => SignatureSize(rsa) - 11;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SignatureSize(this RSA rsa) => rsa.KeySize / 8;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SignatureAndDataSize(this RSA rsa) => rsa.SignatureMaxDataSize() + rsa.SignatureSize();
    }

    public class RSASignedDataStream
    {
        RSACryptoServiceProvider _rsa;
        Stream _stream;
        byte[] _buf;
        byte[] _sig;

        public ISimplexLogger logger { get; set; }

        public RSASignedDataStream(Stream stream, RSACryptoServiceProvider rsa)
        {
            _rsa = rsa;
            _stream = stream;
            _buf = new byte[rsa.SignatureMaxDataSize()];
            _sig = new byte[rsa.SignatureSize()];
        }

        public bool ReadAndVerify(Span<byte> output)
        {
            int numWholeBlocks = output.Length / _rsa.SignatureMaxDataSize();
            int extraBytes = output.Length % _rsa.SignatureMaxDataSize();

            logger?.Debug($"verifying {output.Length} bytes");
            logger?.Debug($"numWholeBlocks {numWholeBlocks}    extraBytes {extraBytes}");

            int maxdatasize = _rsa.SignatureMaxDataSize();
            int anddatasize = _rsa.SignatureAndDataSize();
            int sigsize = _rsa.SignatureSize();

            bool ReadAndVerifyData(Span<byte> output)
            {
                _stream.Read(_buf, 0, output.Length);
                _stream.Read(_sig, 0, _rsa.SignatureSize());

                logger?.Debug($"output size - {output.Length}");
                logger?.Debug($"_buf - {_buf.AsSpan(0, output.Length).ToHexString()}");
                logger?.Debug($"_sig - {_sig.AsSpan().ToHexString()}");

                if (!_rsa.VerifyData(_buf, 0, output.Length, _sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    Console.WriteLine("failed to verify data!");
                    return false;
                }

                Span<byte> bufSpan = _buf.AsSpan(0, output.Length);
                bufSpan.CopyTo(output);

                return true;
            }

            for (int i = 0; i < numWholeBlocks; i++)
            {
                if (!ReadAndVerifyData(output.Slice(i * _rsa.SignatureMaxDataSize(), _rsa.SignatureMaxDataSize())))
                {
                    Console.WriteLine($"failed on whole block {i}");
                    return false;
                }
            }

            if (!ReadAndVerifyData(output.Slice(numWholeBlocks * _rsa.SignatureMaxDataSize(), extraBytes)))
            {
                Console.WriteLine($"failed on remainder {extraBytes}");
                return false;
            }

            return true;
        }

        public void WriteAndSign(Span<byte> input)
        {
            if (input.Length == 0)
                throw new InvalidOperationException("Input span was empty");

            int numWholeBlocks = input.Length / _rsa.SignatureMaxDataSize();
            int extraBytes = input.Length % _rsa.SignatureMaxDataSize();

            logger?.Debug($"signing {input.Length} bytes");
            logger?.Debug($"numWholeBlocks {numWholeBlocks}    extraBytes {extraBytes}");

            int maxdatasize = _rsa.SignatureMaxDataSize();
            int anddatasize = _rsa.SignatureAndDataSize();
            int sigsize = _rsa.SignatureSize();

            void WriteAndSignData(Span<byte> input)
            {
                input.CopyTo(_buf);
                byte[] signature = _rsa.SignData(_buf, 0, input.Length, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                logger?.Debug($"input size - {input.Length}");
                logger?.Debug($"_buf - {_buf.AsSpan().ToHexString()}");
                logger?.Debug($"signature - {signature.AsSpan().ToHexString()}");

                _stream.Write(_buf, 0, input.Length);
                _stream.Write(signature, 0, _rsa.SignatureSize());
            }

            for (int i = 0; i < numWholeBlocks; i++)
            {
                Span<byte> data = input.Slice(i * _rsa.SignatureMaxDataSize(), _rsa.SignatureMaxDataSize());

                WriteAndSignData(data);
            }

            Span<byte> d = input.Slice(numWholeBlocks * _rsa.SignatureMaxDataSize(), extraBytes);
            WriteAndSignData(d);
        }
    }
}
