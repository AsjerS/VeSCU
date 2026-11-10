namespace VeSCU;

internal enum QualityMode
{
    NearLossless,
    Lossless
}

internal sealed record Codec(
    string Name,
    string Path,
    string Ext,
    AppConfig.InputFormat Format,
    string Size,
    string Compat
)
{
    public static readonly Codec Jxl = new(
        "JPEG XL",
        "cjxl.exe",
        ".jxl",
        AppConfig.InputFormat.Ppm,
        "tiny",
        "low"
    );
    public static readonly Codec Avif = new(
        "AVIF",
        "avifenc.exe",
        ".avif",
        AppConfig.InputFormat.Png,
        "small",
        "medium"
    );
    public static readonly Codec WebP = new(
        "WebP",
        "cwebp.exe",
        ".webp",
        AppConfig.InputFormat.Png,
        "medium",
        "high"
    );
    public static readonly Codec Jpeg = new(
        "JPEG",
        "cjpeg.exe",
        ".jpg",
        AppConfig.InputFormat.Ppm,
        "large",
        "max"
    );
    public static readonly Codec Png = new(
        "PNG",
        "oxipng.exe",
        ".png",
        AppConfig.InputFormat.Png,
        "large",
        "max"
    );

    public EncoderPreset NearLossless(string args) =>
        new(this, new(Path, args, Format, Ext), QualityMode.NearLossless);

    public EncoderPreset Lossless(string args) =>
        new(this, new(Path, args, Format, Ext), QualityMode.Lossless);
}

internal sealed record EncoderPreset(
    Codec Codec,
    AppConfig.EncoderSection Config,
    QualityMode Quality
)
{
    public string DisplayText =>
        $"{Codec.Name} ({Config.Extension}): {Codec.Size} size, {Codec.Compat} compatibility";

    public static readonly EncoderPreset[] All =
    [
        Codec.WebP.NearLossless("-q 90 -m 6 -o {Output} -- -"),
        Codec.Jpeg.NearLossless("-quality 90 -outfile {Output}"),
        Codec.Jxl.NearLossless("-d 1 - {Output}"),
        Codec.Avif.NearLossless("-s 6 -q 85 --input-format png --stdin {Output}"),

        Codec.WebP.Lossless("-z 9 -o {Output} -- -"),
        Codec.Png.Lossless("-o 2 --out {Output} -"),
        Codec.Jxl.Lossless("-d 0 - {Output}"),
    ];
}
