using System.Diagnostics;

namespace VeSCU;

internal sealed class FirstRunDialog : Form
{
    private sealed record Codec(
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

    private sealed record Preset(Codec Codec, string Args, bool IsLossless)
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

    private readonly RadioButton _radioNearLossless = new()
        { Text = "Near-lossless (recommended)", AutoSize = true, Checked = true };
    private readonly RadioButton _radioLossless = new()
        { Text = "Lossless (pixel perfect)", AutoSize = true };
    private readonly RadioButton[] _codecRadios = new RadioButton[4];

    public AppConfig SelectedConfig { get; private set; } = new();

    public FirstRunDialog()
    {
        Text = "Welcome to VeSCU";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowOnly;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16)
        };
        Controls.Add(mainLayout);

        var content = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };
        mainLayout.Controls.Add(content, 0, 0);

        content.Controls.Add(CreateQualitySection());
        content.Controls.Add(CreateCodecSection());
        mainLayout.Controls.Add(CreateBottomBar(), 0, 1);

        _radioNearLossless.CheckedChanged += (_, _) => UpdateCodecRadios();
        _radioLossless.CheckedChanged += (_, _) => UpdateCodecRadios();
        UpdateCodecRadios();
    }

    private FlowLayoutPanel CreateQualitySection()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(0)
        };

        panel.Controls.Add(new Label
        {
            Text = "Image quality:",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        });

        var modePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(4, 0, 0, 10)
        };
        modePanel.Controls.Add(_radioNearLossless);
        modePanel.Controls.Add(_radioLossless);
        panel.Controls.Add(modePanel);

        return panel;
    }

    private FlowLayoutPanel CreateCodecSection()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(0)
        };

        panel.Controls.Add(new Label
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
        panel.Controls.Add(codecPanel);

        for (int i = 0; i < _codecRadios.Length; i++)
        {
            _codecRadios[i] = new RadioButton { AutoSize = true };
            codecPanel.Controls.Add(_codecRadios[i]);
        }

        return panel;
    }

    private TableLayoutPanel CreateBottomBar()
    {
        var bottomBar = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 14, 0, 0)
        };
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var link = new LinkLabel
        {
            Text = "Detailed comparison ↗",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(
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
        btn.Click += OnSaveAndContinue;
        bottomBar.Controls.Add(btn, 1, 0);
        AcceptButton = btn;

        return bottomBar;
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

    private void OnSaveAndContinue(object? sender, EventArgs e)
    {
        var checkedRadio = _codecRadios.FirstOrDefault(r => r.Visible && r.Checked);
        if (checkedRadio?.Tag is not Preset chosen) return;

        var defaults = new AppConfig();
        SelectedConfig = new AppConfig
        {
            Saving = defaults.Saving with { Extension = chosen.Codec.SavingExt },
            Encoder = new(chosen.Codec.EncoderPath, chosen.Args, chosen.Codec.Format)
        };
    }
}
