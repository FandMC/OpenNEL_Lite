using System;
using Codexus.Cipher.Protocol;

namespace OpenNEL_Lite.type;


internal static class AppState
{
    private static readonly Lazy<Com4399> _com4399 = new Lazy<Com4399>(() => new Com4399());

    private static readonly Lazy<WPFLauncher> _x19 = new Lazy<WPFLauncher>(() => new WPFLauncher());
    
    public static Services? Services;

    public static bool Debug;

    public static Com4399 Com4399 => _com4399.Value;

    public static WPFLauncher X19 => _x19.Value;
}
