using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;
using ImpromptuNinjas.ZStd;

namespace WoG2Tools;

public class BoyImage
{
    public const uint Magic = 0x69796F62; // "boyi"

    public uint Version { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort UnusedWidth { get; set; }
    public ushort UnusedHeight { get; set; }
    private byte[] _ktxHeaderData;

    public ushort MaskWidth;
    public ushort MaskHeight;
    private byte[] _maskImageData;

    public void Read(Stream stream)
    {
        var bs = new BinaryStream(stream, ByteConverter.Little);
        if (bs.ReadUInt32() != Magic)
            throw new IOException("Invalid magic/not a boyimage file.");

        uint version = bs.ReadUInt32();
        Width = bs.ReadUInt16();
        Height = bs.ReadUInt16();
        UnusedWidth = bs.ReadUInt16();
        UnusedHeight = bs.ReadUInt16();

        uint ktxCompressedSize = bs.ReadUInt32();
        uint ktxDecompressedSize = bs.ReadUInt32();

        MaskWidth = bs.ReadUInt16();
        MaskHeight = bs.ReadUInt16();
        uint compressedMaskSize = bs.ReadUInt32();
        uint decompressedMaskSize = bs.ReadUInt32();

        byte[] ktxCompressedData = bs.ReadBytes((int)ktxCompressedSize);
        byte[] ktxDecompressedData = new byte[ktxDecompressedSize];

        var decompressor = new ZStdDecompressor();
        if (decompressor.Decompress(ktxDecompressedData, ktxCompressedData) == ktxDecompressedSize)
        {
            _ktxHeaderData = ktxDecompressedData;
        }
        else
        {
            throw new IOException("Failed to decompress texture/ktx data - decompressed size did not match expected size");
        }

        if (decompressedMaskSize != 0)
        {
            byte[] compressedMaskData = bs.ReadBytes((int)compressedMaskSize);
            byte[] decompressedMaskData = new byte[decompressedMaskSize];
            if (decompressor.Decompress(decompressedMaskData, compressedMaskData) == decompressedMaskSize)
            {
                _maskImageData = decompressedMaskData;
            }
            else
            {
                throw new IOException("Failed to decompress mask texture data - decompressed size did not match expected size");
            }
        }
    }

    public byte[] GetKtxHeader()
    {
        return _ktxHeaderData;
    }

    public byte[] GetMaskData()
    {
        return _maskImageData;
    }
}
