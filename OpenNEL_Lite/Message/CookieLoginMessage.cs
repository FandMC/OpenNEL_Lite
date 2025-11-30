using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.Entities;

namespace OpenNEL_Lite.Message.Login;

internal class CookieLoginMessage : IWsMessage
{
    public string Type => "cookie_login";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var cookie = root.TryGetProperty("cookie", out var ck) ? ck.GetString() : string.Empty;
        EntityX19CookieRequest req;
        try
        {
            req = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookie ?? string.Empty) ?? new EntityX19CookieRequest { Json = cookie ?? string.Empty };
        }
        catch
        {
            req = new EntityX19CookieRequest { Json = cookie ?? string.Empty };
        }
        var (authOtp, channel) = OpenNEL_Lite.type.AppState.X19.LoginWithCookie(req);
        UserManager.Instance.AddUserToMaintain(authOtp);
        UserManager.Instance.AddUser(new OpenNEL_Lite.Entities.Web.EntityUser
        {
            UserId = authOtp.EntityId,
            Authorized = true,
            AutoLogin = false,
            Channel = channel,
            Type = "cookie",
            Details = cookie ?? string.Empty
        }, channel == "netease");
        var list = new System.Collections.ArrayList();
        list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        list.Add(new { type = "accounts", items });
        return list;
    }
}
