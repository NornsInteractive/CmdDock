using Microsoft.Windows.Widgets.Providers;
using System.Runtime.InteropServices;

namespace CmdDock.Widget.COM;

internal static class Guids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
    public const string WidgetProviderClsid = "B6A68D64-323A-4E38-A372-2D99BE1F1E85";
}

[ComImport, ComVisible(false), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(Guids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
    [PreserveSig]
    int LockServer(bool fLock);
}

[ComVisible(true)]
internal class WidgetProviderFactory<T> : IClassFactory where T : IWidgetProvider, new()
{
    private const int CLASS_E_NOAGGREGATION = -2147221232;
    private const int E_NOINTERFACE = -2147467262;

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        try
        {
            Program.Log($"[WidgetProviderFactory] CreateInstance requested. riid={riid}, typeof(T).GUID={typeof(T).GUID}");
            if (pUnkOuter != IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
            }

            if (riid == typeof(T).GUID 
                || riid == Guid.Parse(Guids.IUnknown)
                || riid == typeof(IWidgetProvider).GUID
                || riid == typeof(IWidgetProvider2).GUID
                || riid == Guid.Parse("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90"))
            {
                ppvObject = WinRT.MarshalInspectable<IWidgetProvider>.FromManaged(new T());
                Program.Log($"[WidgetProviderFactory] Successfully created instance: {ppvObject}");
                return 0;
            }
            else
            {
                Program.Log($"[WidgetProviderFactory] E_NOINTERFACE for riid={riid}");
                Marshal.ThrowExceptionForHR(E_NOINTERFACE);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[WidgetProviderFactory] Exception in CreateInstance: {ex}");
            throw;
        }

        return 0;
    }

    int IClassFactory.LockServer(bool fLock) => 0;
}
