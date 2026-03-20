using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

namespace WindowsMMBClip;

internal sealed class SelectionTracker : IDisposable
{
    private readonly System.Windows.Forms.Timer _safetyProbeTimer;
    private readonly AutomationEventHandler _selectionChangedHandler;
    private bool _started;
    private string? _lastPublishedSelection;
    private AutomationElement? _trackedElement;

    public event EventHandler<string>? PrimaryTextDetected;

    public SelectionTracker()
    {
        _selectionChangedHandler = (_, _) => PublishSelectionFromTrackedElement();
        _safetyProbeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _safetyProbeTimer.Tick += (_, _) =>
        {
            SwitchTrackedElement();
            PublishSelectionFromTrackedElement();
        };
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        Automation.AddAutomationFocusChangedEventHandler(OnFocusChanged);
        _safetyProbeTimer.Start();
        SwitchTrackedElement(force: true);
        PublishSelectionFromTrackedElement();
    }

    private void OnFocusChanged(object sender, AutomationFocusChangedEventArgs e)
    {
        SwitchTrackedElement(force: true);
        PublishSelectionFromTrackedElement();
    }

    private void SwitchTrackedElement(bool force = false)
    {
        try
        {
            AutomationElement? focused = GetFocusedElementWithCache();
            if (focused == null) { DetachTrackedElement(); return; }
            if (!force && _trackedElement != null && Automation.Compare(_trackedElement, focused)) return;

            DetachTrackedElement();
            if (IsPasswordControl(focused) || !TryGetTextPattern(focused, out _)) return;

            _trackedElement = focused;
            Automation.AddAutomationEventHandler(
                TextPattern.TextSelectionChangedEvent,
                _trackedElement,
                TreeScope.Element,
                _selectionChangedHandler);
        }
        catch { DetachTrackedElement(); }
    }

    private void PublishSelectionFromTrackedElement()
    {
        if (_trackedElement == null) return;

        try
        {
            if (!TryGetTextPattern(_trackedElement, out TextPattern? textPattern) || textPattern == null) return;

            string selectedText = NormalizeSelection(ReadSelection(textPattern));
            if (string.IsNullOrEmpty(selectedText) || selectedText == _lastPublishedSelection) return;

            _lastPublishedSelection = selectedText;
            PrimaryTextDetected?.Invoke(this, selectedText);
        }
        catch { DetachTrackedElement(); }
    }

    private static AutomationElement? GetFocusedElementWithCache()
    {
        var request = new CacheRequest();
        request.Add(AutomationElement.IsPasswordProperty);
        request.Add(AutomationElement.IsTextPatternAvailableProperty);
        request.TreeScope = TreeScope.Element;

        using (request.Activate())
        {
            try { return AutomationElement.FocusedElement; }
            catch { return null; }
        }
    }

    private static bool IsPasswordControl(AutomationElement element)
    {
        try
        {
            object value = element.GetCachedPropertyValue(AutomationElement.IsPasswordProperty, true);
            if (value is bool cachedBool) return cachedBool;
        }
        catch { }

        try
        {
            object value = element.GetCurrentPropertyValue(AutomationElement.IsPasswordProperty, true);
            return value is bool b && b;
        }
        catch { return false; }
    }

    private static bool TryGetTextPattern(AutomationElement element, out TextPattern? textPattern)
    {
        textPattern = null;
        try
        {
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out object patternObject)) return false;
            textPattern = patternObject as TextPattern;
            return textPattern != null;
        }
        catch { return false; }
    }

    private static string ReadSelection(TextPattern textPattern)
    {
        var ranges = textPattern.GetSelection();
        if (ranges == null || ranges.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var range in ranges)
        {
            string part = range.GetText(-1);
            if (!string.IsNullOrEmpty(part)) sb.Append(part);
        }
        return sb.ToString();
    }

    private static string NormalizeSelection(string text) => text.Replace("\0", string.Empty);

    private void DetachTrackedElement()
    {
        if (_trackedElement == null) return;
        try { Automation.RemoveAutomationEventHandler(TextPattern.TextSelectionChangedEvent, _trackedElement, _selectionChangedHandler); }
        catch { }
        finally { _trackedElement = null; }
    }

    public void Dispose()
    {
        if (_started)
        {
            DetachTrackedElement();
            Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChanged);
            _started = false;
        }
        _safetyProbeTimer.Dispose();
    }
}
