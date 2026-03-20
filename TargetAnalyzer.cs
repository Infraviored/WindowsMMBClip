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
                // Prevents double-pasting in modern and legacy terminals that handle MMB natively.
                if (className == "ConsoleWindowClass" || 
                    className == "CASCADIA_HOSTING_WINDOW_CLASS" || 
                    className == "PuTTY" || 
                    className == "mintty")
                {
                    return false; 
                }

                // Native Edit Controls (Fast-Success)
                // Instant Win32 resolution for standard fields, bypassing COM entirely.
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
            
            // Extract cached properties. If a property is unsupported, COM returns a COMException or a NotSupported value. 
            // We must handle casting safely. GetCachedPropertyValue returns an object.
            
            object objControlType = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30003);
            object objIsFocusable = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30009);
            object objIsPassword = element.GetCachedPropertyValue((UIA_PROPERTY_ID)30019);
            
            int controlType = objControlType is int i ? i : 0;
            bool isFocusable = objIsFocusable is bool b1 && b1;
            bool isPassword = objIsPassword is bool b2 && b2;
            
            // 50004 = Edit, 50030 = Document
            bool isTextControl = (controlType == 50004 || controlType == 50030);

            if (!isFocusable || isPassword || !isTextControl)
            {
                return false;
            }

            // If it has a Value pattern, ensure it's not explicitly marked read-only.
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
            // If COM fails or times out, fail safely and yield the click to the OS.
            return false;
        }
    }
}
