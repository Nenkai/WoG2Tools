using CommandLine;

using KtxSharp;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WoG2Tools;

internal class Program
{
    public const string Version = "1.0.0";

    static void Main(string[] args)
    {
        Console.WriteLine($"Wog2Tools {Version} by Nenkai");
        Console.WriteLine("- https://github.com/Nenkai");
        Console.WriteLine("- https://twitter.com/Nenkaai");
        Console.WriteLine();

        if (args.Length == 1)
        {
            if (Directory.Exists(args[0]))
            {
                foreach (var file in Directory.GetFiles(args[0]))
                {
                    try
                    {
                        ProcessImage(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to process {file}: {ex.Message}");
                    }
                }
            }
            else
                ProcessImage(args[0]);
               
        }
        else
        {
            var p = Parser.Default.ParseArguments<ImageToPngVerbs>(args)
                   .WithParsed<ImageToPngVerbs>(ImageToPng);
        }
    }

    public static void ImageToPng(ImageToPngVerbs verbs)
    {
        if (verbs.InputPaths.Count() == 1 && Directory.Exists(verbs.InputPaths.First()))
        {
            foreach (var file in Directory.GetFiles(verbs.InputPaths.First(), "*", verbs.Recursive ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
            {
                try
                {
                    ProcessImage(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Skipped: {file} - {e.Message}");
                }
            }
        }
        else
        {
            foreach (var file in verbs.InputPaths)
            {
                try
                {
                    ProcessImage(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Skipped: {file} - {e.Message}");
                }
            }
        }
    }

    private static void ProcessImage(string file)
    {
        using var imageStream = new FileStream(file, FileMode.Open);
        Console.WriteLine($"Processing: {file}");
        try
        {
            var boyImage = new BoyImage();
            boyImage.Read(imageStream);

            Console.WriteLine($"- Original Dimensions: {boyImage.OriginalWidth}x{boyImage.OriginalHeight}");
            Console.WriteLine($"- Pad Dimensions: {boyImage.Width}x{boyImage.Height}");

            byte[] imageData = boyImage.GetKtxHeader();

            KtxStructure ktxStructure = null;
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                ktxStructure = KtxLoader.LoadInput(ms);
                Console.WriteLine($"- KTX Pixel Format: {ktxStructure.header.glPixelFormat}");

                byte[] textureData = ktxStructure.textureData.textureDataOfMipmapLevel[0];

                Image img;
                switch (ktxStructure.header.glPixelFormat)
                {
                    case GlPixelFormat.GL_RGBA:
                        img = Image.LoadPixelData<Rgba32>(textureData, boyImage.Width, boyImage.Height);
                        break;
                    case GlPixelFormat.GL_RGB:
                        img = Image.LoadPixelData<Rgb24>(textureData, boyImage.Width, boyImage.Height);
                        break;
                    default:
                        throw new NotImplementedException($"Ktx format {ktxStructure.header.glPixelFormat} not yet supported.");
                }

                string output = Path.ChangeExtension(file, ".png");
                Console.WriteLine($"Exporting -> {output}");

                img.Save(Path.ChangeExtension(file, ".png"));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERROR: {file} - {e.Message}");
        }
    }

    [Verb("image-to-png", HelpText = "Converts a .image to .png")]
    public class ImageToPngVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public IEnumerable<string> InputPaths { get; set; }

        [Option('r', "recursive", HelpText = "If a folder is provided, whether to recursively convert.")]
        public bool Recursive { get; set; }

    }
}
