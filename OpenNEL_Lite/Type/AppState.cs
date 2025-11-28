using Codexus.Cipher.Protocol;

namespace OpenNEL_Lite.type;


internal static class AppState
{
    public static readonly Com4399 Com4399 = new Com4399();

    public static readonly WPFLauncher X19 = new WPFLauncher();
    
    public static Services? Services;

    public static bool Debug;
}
