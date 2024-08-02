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
    public ushort OriginalWidth { get; set; }
    public ushort OriginalHeight { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    private byte[] _imageData;

    public void Read(Stream stream)
    {
        var bs = new BinaryStream(stream, ByteConverter.Little);
        if (bs.ReadUInt32() != Magic)
            throw new IOException("Invalid magic/not a boyimage file.");

        uint version = bs.ReadUInt32();
        OriginalWidth = bs.ReadUInt16();
        OriginalHeight = bs.ReadUInt16();
        Width = bs.ReadUInt16();
        Height = bs.ReadUInt16();
        uint compressedSize = bs.ReadUInt32();
        uint decompressedSize = bs.ReadUInt32();

        bs.Position += 0x0C;
        byte[] compressedData = bs.ReadBytes((int)compressedSize);
        byte[] outputBuffer = new byte[decompressedSize];

        var decompressor = new ZStdDecompressor();
        if (decompressor.Decompress(outputBuffer, compressedData) == decompressedSize)
        {
            _imageData = outputBuffer;
        }
        else
        {
            throw new IOException("Failed to decompress file - decompressed size did not match expected size");
        }
    }

    public byte[] GetKtxHeader()
    {
        if (_imageData is null)
            throw new ArgumentNullException("No image data, no image has been loaded.");

        return _imageData;
    }
}
