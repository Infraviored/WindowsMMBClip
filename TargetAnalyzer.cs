using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.Foundation;

namespace UbuntuPrimaryClipboard;

internal static class TargetAnalyzer
{
    // Maintain a static singleton to avoid COM initialization overhead on every click
    private static readonly IUIAutomation _automation;
    private static readonly IUIAutomationCacheRequest _cacheRequest;

    // Spatial-Temporal Cache to prevent lag when spamming clicks
    private static int _lastX = -1;
    private static int _lastY = -1;
    private static bool _lastResult;
    private static DateTime _lastCheckTime = DateTime.MinValue;
    private const int SpatialJitter = 2; // Allow 2 pixels of movement
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMilliseconds(500);

    static TargetAnalyzer()
    {
        _automation = (IUIAutomation)new CUIAutomation8();
        _cacheRequest = _automation.CreateCacheRequest();
        
        // Pre-fetch only the properties we need for the decision matrix
        _cacheRequest.AddProperty((UIA_PROPERTY_ID)30003); // UIA_ControlTypePropertyId
        _cacheRequest.AddProperty((UIA_PROPERTY_ID)30009); // UIA_IsKeyboardFocusablePropertyId
        _cacheRequest.AddProperty((UIA_PROPERTY_ID)30019); // UIA_IsPasswordPropertyId
        _cacheRequest.AddProperty((UIA_PROPERTY_ID)30043); // UIA_IsValuePatternAvailablePropertyId
        _cacheRequest.AddProperty((UIA_PROPERTY_ID)30046); // UIA_ValueIsReadOnlyPropertyId
    }

    public static bool IsEditableElementAtPoint(int x, int y)
    {
        // 1. Check Spatial-Temporal Cache (Avoids "spam" lag)
        // If we clicked here recently, reuse the result without calling COM.
        TimeSpan timeSinceLast = DateTime.Now - _lastCheckTime;
        if (timeSinceLast < CacheExpiry && 
            Math.Abs(x - _lastX) <= SpatialJitter && 
            Math.Abs(y - _lastY) <= SpatialJitter)
        {
            return _lastResult;
        }

        bool result = PerformContextCheck(x, y);

        // Update cache
        _lastX = x;
        _lastY = y;
        _lastResult = result;
        _lastCheckTime = DateTime.Now;

        return result;
    }

    private static bool PerformContextCheck(int x, int y)
    {
        // STAGE 1: Native Win32 Fast-Path & Terminal Bypass
        var pt = new NativeMethods.POINT { x = x, y = y };
        IntPtr hwnd = NativeMethods.WindowFromPoint(pt);

        if (hwnd != IntPtr.Zero)
        {
            StringBuilder classNameBuilder = new(256);
            if (NativeMethods.GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity) > 0)
            {
                string className = classNameBuilder.ToString();

                // Terminal Bypass (Fast-Fail)
                if (className == "ConsoleWindowClass" || 
                    className == "CASCADIA_HOSTING_WINDOW_CLASS" || 
                    className == "PuTTY" || 
                    className == "mintty")
                {
                    return false; 
                }

                // Native Edit Controls (Fast-Success)
                if (className == "EDIT" || 
                    className == "RichEdit20W" || 
                    className == "RichEdit50W")
                {
                    return true;
                }
            }
        }

        // STAGE 2: COM IUIAutomation Cache Query
        try
        {
            var tagPoint = new System.Drawing.Point(x, y);
            IUIAutomationElement? element = _automation.ElementFromPointBuildCache(tagPoint, _cacheRequest);

            if (element == null)
            {
                return false;
            }

            // STAGE 3: The Decision Matrix
            object objControlType = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30003);
            object objIsFocusable = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30009);
            object objIsPassword = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30019);
            
            int controlType = objControlType is int i ? i : 0;
            bool isFocusable = objIsFocusable is bool b1 && b1;
            bool isPassword = objIsPassword is bool b2 && b2;
            
            bool isTextControl = (controlType == 50004 || controlType == 50030);

            if (!isFocusable || isPassword || !isTextControl)
            {
                return false;
            }

            object objHasValue = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30043);
            if (objHasValue is bool hasValue && hasValue)
            {
                object objIsReadOnly = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30046);
                if (objIsReadOnly is bool isReadOnly && isReadOnly)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
