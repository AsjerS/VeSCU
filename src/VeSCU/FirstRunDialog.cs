using System.Diagnostics;

namespace VeSCU;

public sealed class FirstRunDialog : Form
{
    private record Codec(
        string Name,
        string EncoderPath,
        string SavingExt,
        AppConfig.InputFormat Format,
        string Size,
        string Compat
    );

    private static readonly Codec Jxl = new(
        "JPEG XL",
        "cjxl.exe",
        ".jxl",
        AppConfig.InputFormat.Ppm,
        "tiny",
        "low"
    );

    private static readonly Codec Avif = new(
        "AVIF",
        "avifenc.exe",
        ".avif",
        AppConfig.InputFormat.Ppm,
        "small",
        "medium"
    );

    private static readonly Codec WebP = new(
        "WebP",
        "cwebp.exe",
        ".webp",
        AppConfig.InputFormat.Ppm,
        "medium",
        "high"
    );

    private static readonly Codec Jpeg = new(
        "JPEG",
        "cjpeg.exe",
        ".jpg",
        AppConfig.InputFormat.Ppm,
        "large",
        "max"
    );

    private static readonly Codec Png = new(
        "PNG",
        "oxipng.exe",
        ".png",
        AppConfig.InputFormat.Png,
        "large",
        "max"
    );

    private record Preset(Codec Codec, string Args, bool IsLossless)
    {
        public string DisplayText =>
            $"{Codec.Name} ({Codec.SavingExt}): {Codec.Size} size, {Codec.Compat} compatibility";
    }

    private static readonly Preset[] Presets = [
        new(Jxl, "-d 1 - {Output}", IsLossless: false),
        new(Avif, "-s 6 -q 85 - {Output}", IsLossless: false),
        new(WebP, "-q 90 -m 6 -o {Output} -- -", IsLossless: false),
        new(Jpeg, "-quality 90 -outfile {Output}", IsLossless: false),

        new(Jxl, "-d 0 - {Output}", IsLossless: true),
        new(WebP, "-z 9 -o {Output} -- -", IsLossless: true),
        new(Png, "-o 2 --out {Output} -", IsLossless: true)
    ];

    private readonly RadioButton _radioNearLossless;
    private readonly RadioButton _radioLossless;
    private readonly RadioButton[] _codecRadios = new RadioButton[4];

    public AppConfig SelectedConfig { get; private set; } = new();

    public FirstRunDialog()
    {
        Text = "Welcome to VeSCU";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        ClientSize = LogicalToDeviceUnits(new Size(330, 260));

        var bottomBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 8, 16, 16)
        };
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Controls.Add(bottomBar);

        var link = new LinkLabel
        {
            Text = "View detailed comparison ↗",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        link.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo(
            "https://jpegxl.info/resources/battle-of-codecs.html"
        )
        { UseShellExecute = true });
        bottomBar.Controls.Add(link, 0, 0);

        var btn = new Button
        {
            Text = "Save and Continue",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
        };
        btn.Click += (s, e) =>
        {
            var checkedRadio = _codecRadios.FirstOrDefault(r => r.Visible && r.Checked);
            if (checkedRadio?.Tag is not Preset chosen) return;

            SelectedConfig = new AppConfig
            {
                Saving = new(
                    Extension: chosen.Codec.SavingExt
                ),
                Encoder = new(
                    Path: chosen.Codec.EncoderPath,
                    Arguments: chosen.Args,
                    InputFormat: chosen.Codec.Format
                )
            };
        };
        bottomBar.Controls.Add(btn, 1, 0);
        AcceptButton = btn;

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(16, 16, 16, 0)
        };
        Controls.Add(content);

        content.Controls.Add(new Label
        {
            Text = "Image quality:",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        });

        var modePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(4, 0, 0, 12)
        };
        _radioNearLossless = new RadioButton
        {
            Text = "Near-lossless (recommended)",
            AutoSize = true,
            Checked = true
        };
        _radioLossless = new RadioButton
        {
            Text = "Lossless (pixel perfect)",
            AutoSize = true
        };
        modePanel.Controls.Add(_radioNearLossless);
        modePanel.Controls.Add(_radioLossless);
        content.Controls.Add(modePanel);

        content.Controls.Add(new Label
        {
            Text = "Image format:",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        });

        var codecPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(4, 0, 0, 0)
        };
        content.Controls.Add(codecPanel);

        for (int i = 0; i < _codecRadios.Length; i++)
        {
            _codecRadios[i] = new RadioButton { AutoSize = true };
            codecPanel.Controls.Add(_codecRadios[i]);
        }

        _radioNearLossless.CheckedChanged += (s, e) => UpdateCodecRadios();
        _radioLossless.CheckedChanged += (s, e) => UpdateCodecRadios();
        UpdateCodecRadios();
    }

    private void UpdateCodecRadios()
    {
        var filtered = Presets.Where(
            p => p.IsLossless == _radioLossless.Checked
        ).ToArray();

        for (int i = 0; i < _codecRadios.Length; i++)
        {
            if (i < filtered.Length)
            {
                _codecRadios[i].Text = filtered[i].DisplayText;
                _codecRadios[i].Tag = filtered[i];
                _codecRadios[i].Visible = true;
            }
            else
            {
                _codecRadios[i].Visible = false;
            }
        }

        if (!_codecRadios.Any(r => r.Visible && r.Checked))
        {
            _codecRadios[0].Checked = true;
        }
    }
}