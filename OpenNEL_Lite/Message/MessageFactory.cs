using OpenNEL_Lite.Message.Game;
using OpenNEL_Lite.Message.Login;
using OpenNEL_Lite.Message.Connected;
using OpenNEL_Lite.Network;

namespace OpenNEL_Lite.Message;

internal static class MessageFactory
{
    private static readonly Dictionary<string, IWsMessage> Map;

    static MessageFactory()
    {
        var login = new LoginMessage();
        var handlers = new IWsMessage[]
        {
            login,
            new DeleteAccountMessage(),
            new GetAccountMessage(),
            new SelectAccountMessage(),
            new OpenServerMessage(),
            new CreateRoleNamedMessage(),
            new JoinGameMessage(),
            new ShutdownGameMessage()
        };
        Map = handlers.ToDictionary(h => h.Type, h => h);
        Map["login_4399"] = login;
        Map["login_x19"] = login;
        Map["cookie_login"] = login;
        Map["activate_account"] = login;
    }

    public static IWsMessage? Get(string type)
    {
        return Map.TryGetValue(type, out var h) ? h : null;
    }
}
