using System.Text.Json;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.X19;

namespace OpenNEL_Lite.Utils;

public static class X19Extensions
{
    private static async Task<HttpResponseMessage> Api(this X19AuthenticationOtp otp, string url, string body)
    {
        return await X19.ApiPostAsync(url, body, otp.EntityId, otp.Token);
    }

    public static async Task<TResult> Api<TBody, TResult>(this X19AuthenticationOtp otp, string url, TBody body)
    {
        var response = await otp.Api(url, JsonSerializer.Serialize(body));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TResult>(json);
        return result!;
    }
}