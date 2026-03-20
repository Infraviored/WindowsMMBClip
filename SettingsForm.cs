using System.Windows.Forms;

namespace WindowsMMBClip;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly TrackBar _pasteSlider;
    private readonly TrackBar _stabilizeSlider;
    private readonly CheckBox _startupCheckbox;
    private readonly Label _pasteLabel;
    private readonly Label _stabilizeLabel;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "WindowsMMBClip Settings";
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new System.Drawing.Size(400, 0);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        var mainPadding = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 2
        };
        mainPadding.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPadding.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 7
        };

        // Paste Delay
        panel.Controls.Add(new Label { Text = "Paste Reliability (ms)", AutoSize = true, Margin = new Padding(0, 0, 0, 5) });
        _pasteLabel = new Label { AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold), Margin = new Padding(5, 0, 0, 0) };
        _pasteSlider = new TrackBar { Minimum = 40, Maximum = 500, TickFrequency = 50, Dock = DockStyle.Fill, Value = settings.PasteDelay, Height = 45 };
        _pasteSlider.ValueChanged += (s, e) => UpdateLabels();
        panel.Controls.Add(_pasteSlider);
        panel.Controls.Add(_pasteLabel);

        panel.Controls.Add(new Label { Text = "System Stability (ms)", AutoSize = true, Margin = new Padding(0, 20, 0, 5) });
        _stabilizeLabel = new Label { AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold), Margin = new Padding(5, 0, 0, 0) };
        _stabilizeSlider = new TrackBar { Minimum = 10, Maximum = 200, TickFrequency = 20, Dock = DockStyle.Fill, Value = settings.StabilizationDelay, Height = 45 };
        _stabilizeSlider.ValueChanged += (s, e) => UpdateLabels();
        panel.Controls.Add(_stabilizeSlider);
        panel.Controls.Add(_stabilizeLabel);

        _startupCheckbox = new CheckBox 
        { 
            Text = "Start WindowsMMBClip with Windows", 
            AutoSize = true, 
            Checked = settings.StartWithWindows,
            Margin = new Padding(0, 20, 0, 10)
        };
        panel.Controls.Add(_startupCheckbox);

        var buttonPanel = new FlowLayoutPanel 
        { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft, 
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 10, 0, 0) 
        };
        
        var closeButton = new Button { Text = "Close", DialogResult = DialogResult.OK, Height = 30, Width = 100 };
        var resetButton = new Button { Text = "Reset Defaults", Height = 30, Width = 120 };
        resetButton.Click += (s, e) =>
        {
            _pasteSlider.Value = 75;
            _stabilizeSlider.Value = 35;
            _startupCheckbox.Checked = false;
        };

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(resetButton);

        mainPadding.Controls.Add(panel);
        mainPadding.Controls.Add(buttonPanel);
        Controls.Add(mainPadding);

        UpdateLabels();

        FormClosing += (s, e) =>
        {
            _settings.PasteDelay = _pasteSlider.Value;
            _settings.StabilizationDelay = _stabilizeSlider.Value;
            _settings.StartWithWindows = _startupCheckbox.Checked;
            _settings.Save();
            StartupManager.SetStartup(_settings.StartWithWindows);
        };
    }

    private void UpdateLabels()
    {
        _pasteLabel.Text = $"{_pasteSlider.Value} ms";
        _stabilizeLabel.Text = $"{_stabilizeSlider.Value} ms";
    }
}
