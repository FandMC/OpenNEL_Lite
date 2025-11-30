using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Protocol;
using OpenNEL_Lite.Entities;
using OpenNEL_Lite.Entities.Web;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;

namespace OpenNEL_Lite.Message.Login;

internal class LoginX19Message : IWsMessage
{
    public string Type => "login_x19";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        try
        {
            var email = root.TryGetProperty("email", out var e) ? e.GetString() : string.Empty;
            var pwd = root.TryGetProperty("password", out var p) ? p.GetString() : string.Empty;
            WPFLauncher x = OpenNEL_Lite.type.AppState.X19;
            EntityX19CookieRequest req = WPFLauncher.GenerateCookie(x.LoginWithEmailAsync(email ?? string.Empty, pwd ?? string.Empty).GetAwaiter().GetResult(), x.MPay.GetDevice());
            var (authOtp, channel) = x.LoginWithCookie(req);
            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = authOtp.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = channel,
                Type = "cookie",
                Details = JsonSerializer.Serialize(req)
            });
            var list = new System.Collections.ArrayList();
            list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (System.Exception ex)
        {
            return new { type = "login_error", message = ex.Message ?? "登录失败" };
        }
    }
}
