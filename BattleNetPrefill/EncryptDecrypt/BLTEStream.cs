using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;

namespace BattleNetPrefill.EncryptDecrypt
{
    [Serializable]
    public class BLTEDecoderException : Exception
    {
        public int ErrorCode { get; }

        public BLTEDecoderException(int error, string message) : base(message)
        {
            ErrorCode = error;
        }
    }

    class DataBlock
    {
        public int CompSize;
        public int DecompSize;
        public MD5Hash Hash;
    }

    public class BLTEStream : Stream
    {
        private BinaryReader _reader;
        private readonly MD5 _md5 = MD5.Create();
        private MemoryStream _memStream;
        private DataBlock[] _dataBlocks;
        private Stream _stream;
        private int _blocksIndex;
        private long _length;
        private bool _hasHeader;

        private const byte ENCRYPTION_SALSA20 = 0x53;
        private const byte ENCRYPTION_ARC4 = 0x41;
        private const int BLTE_MAGIC = 0x45544c42;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get { return _memStream.Position; }
            set
            {
                while (value > _memStream.Length)
                    if (!ProcessNextBlock())
                        break;

                _memStream.Position = value;
            }
        }

        public BLTEStream(Stream src, in MD5Hash eKey)
        {
            _stream = src;
            _reader = new BinaryReader(src);

            Parse(eKey);
        }

        private void Parse(in MD5Hash eKey)
        {
            int size = (int)_reader.BaseStream.Length;

            if (size < 8)
                throw new BLTEDecoderException(0, "not enough data: size < 8");

            int magic = _reader.ReadInt32();

            if (magic != BLTE_MAGIC)
                throw new BLTEDecoderException(0, "frame header mismatch (bad BLTE file)");

            int headerSize = _reader.ReadInt32BE();
            _hasHeader = headerSize > 0;

            if (Config.ValidateData)
            {
                long oldPos = _reader.BaseStream.Position;

                _reader.BaseStream.Position = 0;

                byte[] newHash = _md5.ComputeHash(_reader.ReadBytes(_hasHeader ? headerSize : size));

                if (!eKey.EqualsTo9(newHash))
                    throw new BLTEDecoderException(0, "data corrupted");

                _reader.BaseStream.Position = oldPos;
            }

            int numBlocks = 1;

            if (_hasHeader)
            {
                if (size < 12)
                    throw new BLTEDecoderException(0, "not enough data: size < 12");

                byte[] fcbytes = _reader.ReadBytes(4);

                numBlocks = fcbytes[1] << 16 | fcbytes[2] << 8 | fcbytes[3] << 0;

                if (fcbytes[0] != 0x0F || numBlocks == 0)
                    throw new BLTEDecoderException(0, $"bad table format 0x{fcbytes[0]:x2}, numBlocks {numBlocks}");

                int frameHeaderSize = 24 * numBlocks + 12;

                if (headerSize != frameHeaderSize)
                    throw new BLTEDecoderException(0, "header size mismatch");

                if (size < frameHeaderSize)
                    throw new BLTEDecoderException(0, "not enough data: size < frameHeaderSize");
            }

            _dataBlocks = new DataBlock[numBlocks];

            for (int i = 0; i < numBlocks; i++)
            {
                DataBlock block = new DataBlock();

                if (_hasHeader)
                {
                    block.CompSize = _reader.ReadInt32BE();
                    block.DecompSize = _reader.ReadInt32BE();
                    block.Hash = _reader.Read<MD5Hash>();
                }
                else
                {
                    block.CompSize = size - 8;
                    block.DecompSize = size - 8 - 1;
                    block.Hash = default;
                }

                _dataBlocks[i] = block;
            }

            _memStream = new MemoryStream(_dataBlocks.Sum(b => b.DecompSize));

            ProcessNextBlock();

            _length = _hasHeader ? _memStream.Capacity : _memStream.Length;

            //for (int i = 0; i < _dataBlocks.Length; i++)
            //{
            //    ProcessNextBlock();
            //}
        }

        private bool ProcessNextBlock()
        {
            if (_blocksIndex == _dataBlocks.Length)
                return false;

            long oldPos = _memStream.Position;
            _memStream.Position = _memStream.Length;

            DataBlock block = _dataBlocks[_blocksIndex];

            long startPos = _stream.Position;

            using (NestedStream ns = new NestedStream(_stream, block.CompSize, true))
            {
                if (_hasHeader && Config.ValidateData)
                {
                    byte[] blockHash = _md5.ComputeHash(ns);

                    if (!block.Hash.EqualsTo(blockHash))
                        throw new BLTEDecoderException(0, "MD5 mismatch");

                    ns.Position = 0;
                }

                HandleDataBlock(ns, _blocksIndex);
            }

            _stream.Position = startPos + block.CompSize;

            _blocksIndex++;

            _memStream.Position = oldPos;

            return true;
        }

        private void HandleDataBlock(Stream data, int index)
        {
            byte blockType = (byte)data.ReadByte();
            switch (blockType)
            {
                case 0x45: // E (encrypted)
                    Stream decryptedData = Decrypt(data, index);
                    if (decryptedData != null)
                        using (decryptedData)
                            HandleDataBlock(decryptedData, index);
                    else
                        _memStream.Write(new byte[_dataBlocks[index].DecompSize], 0, _dataBlocks[index].DecompSize);
                    break;
                case 0x46: // F (frame, recursive)
                    throw new BLTEDecoderException(1, "DecoderFrame not implemented");
                case 0x4E: // N (not compressed)
                    data.CopyTo(_memStream);
                    break;
                case 0x5A: // Z (zlib compressed)
                    Decompress(data, _memStream);
                    break;
                default:
                    throw new BLTEDecoderException(1, $"unknown BLTE block type {(char)blockType} (0x{blockType:X2})!");
            }
        }

        private static Stream Decrypt(Stream data, int index)
        {
            using (BinaryReader br = new BinaryReader(data))
            {
                byte keyNameSize = br.ReadByte();

                if (keyNameSize == 0 || keyNameSize != 8)
                    throw new BLTEDecoderException(2, "keyNameSize == 0 || keyNameSize != 8");

                ulong keyName = br.ReadUInt64();

                byte IVSize = br.ReadByte();

                if (IVSize != 4 || IVSize > 0x10)
                    throw new BLTEDecoderException(2, "IVSize != 4 || IVSize > 0x10");

                byte[] IV = br.ReadBytes(IVSize);
                // expand to 8 bytes
                Array.Resize(ref IV, 8);

                if (data.Length < keyNameSize + IVSize + 4)
                    throw new BLTEDecoderException(2, "data.Length < IVSize + keyNameSize + 4");

                byte encType = br.ReadByte();

                if (encType != ENCRYPTION_SALSA20 && encType != ENCRYPTION_ARC4) // 'S' or 'A'
                    throw new BLTEDecoderException(2, "encType != ENCRYPTION_SALSA20 && encType != ENCRYPTION_ARC4");

                // magic
                for (int shift = 0, i = 0; i < sizeof(int); shift += 8, i++)
                {
                    IV[i] ^= (byte)((index >> shift) & 0xFF);
                }

                byte[] key = KeyService.GetKey(keyName);
                bool hasKey = key != null;

                if (key == null)
                {
                    key = new byte[16];
                    if (index == 0)
                    {
                        throw new BLTEDecoderException(3, $"unknown keyname {keyName:X16}");
                    }
                }

                if (encType == ENCRYPTION_SALSA20)
                {
                    using (ICryptoTransform decryptor = KeyService.SalsaInstance.CreateDecryptor(key, IV))
                    using (CryptoStream cs = new CryptoStream(data, decryptor, CryptoStreamMode.Read))
                    {
                        MemoryStream ms = cs.CopyToMemoryStream();
                        return hasKey ? ms : null;
                    }
                }
                else
                {
                    // ARC4 ?
                    throw new BLTEDecoderException(2, "encType ENCRYPTION_ARC4 not implemented");
                }
            }
        }

        private static void Decompress(Stream data, Stream outStream)
        {
#if NET6_0_OR_GREATER
            using (var zlibStream = new ZLibStream(data, CompressionMode.Decompress))
            {
                zlibStream.CopyTo(outStream);
            }
#else
            // skip first 2 bytes (zlib)
            data.Position += 2;
            using (var dfltStream = new DeflateStream(data, CompressionMode.Decompress))
            {
                dfltStream.CopyTo(outStream);
            }
#endif
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_memStream.Position + count > _memStream.Length && _blocksIndex < _dataBlocks.Length)
            {
                if (!ProcessNextBlock())
                    return 0;

                return Read(buffer, offset, count);
            }
            else
            {
                return _memStream.Read(buffer, offset, count);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _stream?.Dispose();
                _reader?.Dispose();
                _memStream?.Dispose();
            }
            finally
            {
                _stream = null;
                _reader = null;
                _memStream = null;

                base.Dispose(disposing);
            }
        }
    }
}
