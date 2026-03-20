using System.Windows.Forms;

namespace WindowsMMBClip;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly TrackBar _pasteSlider;
    private readonly TrackBar _stabilizeSlider;
    private readonly Label _pasteLabel;
    private readonly Label _stabilizeLabel;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "WindowsMMBClip Settings";
        Size = new System.Drawing.Size(350, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = IconGenerator.Generate();

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(20)
        };

        // Paste Delay
        panel.Controls.Add(new Label { Text = "Paste Reliability (ms)", AutoSize = true });
        _pasteLabel = new Label { AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) };
        _pasteSlider = new TrackBar { Minimum = 40, Maximum = 500, TickFrequency = 50, Dock = DockStyle.Fill, Value = settings.PasteDelay };
        _pasteSlider.ValueChanged += (s, e) => UpdateLabels();
        panel.Controls.Add(_pasteSlider);
        panel.Controls.Add(_pasteLabel);

        panel.Controls.Add(new Label { Text = "System Stability (ms)", AutoSize = true, Margin = new Padding(0, 15, 0, 0) });
        _stabilizeLabel = new Label { AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) };
        _stabilizeSlider = new TrackBar { Minimum = 10, Maximum = 200, TickFrequency = 20, Dock = DockStyle.Fill, Value = settings.StabilizationDelay };
        _stabilizeSlider.ValueChanged += (s, e) => UpdateLabels();
        panel.Controls.Add(_stabilizeSlider);
        panel.Controls.Add(_stabilizeLabel);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40 };
        var closeButton = new Button { Text = "Close", DialogResult = DialogResult.OK };
        var resetButton = new Button { Text = "Reset Defaults" };
        resetButton.Click += (s, e) =>
        {
            _pasteSlider.Value = 75;
            _stabilizeSlider.Value = 35;
        };

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(resetButton);

        Controls.Add(panel);
        Controls.Add(buttonPanel);

        UpdateLabels();

        FormClosing += (s, e) =>
        {
            _settings.PasteDelay = _pasteSlider.Value;
            _settings.StabilizationDelay = _stabilizeSlider.Value;
            _settings.Save();
        };
    }

    private void UpdateLabels()
    {
        _pasteLabel.Text = $"{_pasteSlider.Value} ms";
        _stabilizeLabel.Text = $"{_stabilizeSlider.Value} ms";
    }
}
