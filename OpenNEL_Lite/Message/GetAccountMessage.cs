using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenNEL_Lite.Entities;
using OpenNEL_Lite.Entities.Web;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;

namespace OpenNEL_Lite.Message.Connected;

public class GetAccountMessage : IWsMessage
{
    public string Type => "list_accounts";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        return new { type = "accounts", items };
    }
}
