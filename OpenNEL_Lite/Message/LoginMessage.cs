using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Protocol;
using OpenNEL_Lite.Entities;
using OpenNEL_Lite.Entities.Web;
using OpenNEL_Lite.Entities.Web.NEL;
using OpenNEL_Lite.Enums;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;
using Serilog;

namespace OpenNEL_Lite.Message.Login;

public class LoginMessage : IWsMessage
{
    public string Type => "login";
    private EntityLoginRequest? _entity;
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        try
        {
            string cookie = root.GetString() ?? string.Empty;
            
            EntityX19CookieRequest req;
            try
            {
                req = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookie) ?? new EntityX19CookieRequest { Json = cookie};
            }
            catch
            {
                req = new EntityX19CookieRequest { Json = cookie };
            }
            var (authOtp, channel) = type.AppState.X19.LoginWithCookie(req);
            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new EntityUser
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
            var items = UserManager.Instance.GetUsersNoDetails().Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (ArgumentNullException)
        {
            return new { type = "login_error", message = "当前cookie过期了" };
        }
        catch (Exception ex)
        {
            return new { type = "login_error", message = ex.Message};
        }
    }
}
