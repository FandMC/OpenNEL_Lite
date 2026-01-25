using System;
using System.Text.Json;
using OpenNEL_Lite.Entities.Web;
using OpenNEL_Lite.Entities.Web.NEL;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;
using Serilog;

namespace OpenNEL_Lite.Message.Login;

internal class ActivateAccountMessage : IWsMessage
{
    public string Type => "activate_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var idp) ? idp.GetString() : (root.TryGetProperty("entityId", out var idp2) ? idp2.GetString() : null);
        if (string.IsNullOrWhiteSpace(id)) return new { type = "activate_account_error", message = "缺少id" };
        var u = UserManager.Instance.GetUserByEntityId(id!);
        if (u == null)
        {
            return new { type = "activate_account_error", message = "账号不存在" };
        }
        try
        {
            if (!u.Authorized)
            {
                var result = ReLoginByType(u);
                var tProp = result?.GetType().GetProperty("type");
                var tVal = tProp?.GetValue(result) as string;
                if (tVal == "captcha_required")
                {
                    Log.Information("[ActivateAccount] 需要验证码");
                    return result;
                }
                if (tVal != null && tVal.EndsWith("_error", StringComparison.OrdinalIgnoreCase))
                {
                    var mProp = result?.GetType().GetProperty("message");
                    var msg = mProp?.GetValue(result) as string ?? "登录失败";
                    Log.Error("[ActivateAccount] 登录失败: {Msg}", msg);
                    return result;
                }
                u.Authorized = true;
            }
            var list = new System.Collections.ArrayList();
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(x => new { entityId = x.UserId, channel = x.Channel, status = x.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (Codexus.Cipher.Utils.Exception.CaptchaException)
        {
            if (u.Type?.ToLowerInvariant() == "password")
                return HandleCaptchaRequired(u);
            return new { type = "activate_account_error", message = "登录失败" };
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var lower = msg.ToLowerInvariant();
            if (lower.Contains("parameter") && lower.Contains("'s'") && u.Type?.ToLowerInvariant() == "password")
            {
                return HandleCaptchaRequired(u);
            }
            return new { type = "activate_account_error", message = msg.Length == 0 ? "激活失败" : msg };
        }
    }

    private object ReLoginByType(EntityUser u)
    {
        var userType = u.Type?.ToLowerInvariant() ?? string.Empty;
        switch (userType)
        {
            case "password":
                var pwdReq = JsonSerializer.Deserialize<EntityPasswordRequest>(u.Details);
                if (pwdReq == null) throw new Exception("无法解析4399登录信息");
                return new Login4399Message().ProcessAsync(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { account = pwdReq.Account, password = pwdReq.Password })));
            case "netease":
                var neteaseReq = JsonSerializer.Deserialize<NeteaseLoginInfo>(u.Details);
                if (neteaseReq == null) throw new Exception("无法解析网易登录信息");
                return new LoginX19Message().ProcessAsync(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { email = neteaseReq.Email, password = neteaseReq.Password })));
            case "cookie":
                return new LoginMessage().ProcessAsync(JsonSerializer.Deserialize<JsonElement>(u.Details));
            default:
                throw new Exception($"不支持的账号类型: {u.Type}");
        }
    }

    private class NeteaseLoginInfo
    {
        public string Email { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
    }

    private object HandleCaptchaRequired(EntityUser u)
    {
        try
        {
            var req = JsonSerializer.Deserialize<EntityPasswordRequest>(u.Details);
            var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
            var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;

            return new { type = "captcha_required", account = req?.Account ?? string.Empty, password = req?.Password ?? string.Empty, sessionId = captchaSid, captchaUrl = url };
        }
        catch
        {
            return new { type = "captcha_required" };
        }
    }
}
