using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.Manager;
using OpenNEL_Lite.Entities.Web;
using OpenNEL_Lite.Entities.Web.NEL;
using Serilog;

namespace OpenNEL_Lite.Manager;

public class UserManager : IUserManager
{
    private const int ExpirationMinutes = 30;

    private const int CheckIntervalMs = 2000;

	private static readonly SemaphoreSlim InstanceLock = new SemaphoreSlim(1, 1);

	private static UserManager? _instance;

	private readonly ConcurrentDictionary<string, EntityUser> _users = new ConcurrentDictionary<string, EntityUser>();

	private readonly ConcurrentDictionary<string, EntityAvailableUser> _availableUsers = new ConcurrentDictionary<string, EntityAvailableUser>();

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private volatile bool _isDirty;

    public static UserManager Instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}
			InstanceLock.Wait();
			try
			{
				return _instance ?? (_instance = new UserManager());
			}
			finally
			{
				InstanceLock.Release();
			}
		}
	}

    private UserManager()
    {
        IUserManager.Instance = this;
        Task.Run((Func<Task?>)MaintainThreadAsync, _cancellationTokenSource.Token);
    }

	public EntityAvailableUser? GetAvailableUser(string entityId)
	{
		if (!_availableUsers.TryGetValue(entityId, out EntityAvailableUser value))
		{
			return null;
		}
		return value;
	}

	private async Task MaintainThreadAsync()
	{
		using WPFLauncher launcher = new WPFLauncher();
		_ = 2;
		try
		{
			while (!_cancellationTokenSource.Token.IsCancellationRequested)
			{
				try
				{
					await ProcessExpiredUsersAsync(launcher);
                    await Task.Delay(CheckIntervalMs, _cancellationTokenSource.Token);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception exception)
				{
                    Log.Error(exception, "维护线程迭代错误");
                    await Task.Delay(CheckIntervalMs);
				}
			}
		}
		catch (OperationCanceledException)
		{
            Log.Information("维护线程已取消");
		}
		catch (Exception exception2)
		{
            Log.Error(exception2, "维护线程发生致命错误");
		}
	}

	private async Task ProcessExpiredUsersAsync(WPFLauncher launcher)
	{
        long expirationThreshold = DateTimeOffset.UtcNow.AddMinutes(-ExpirationMinutes).ToUnixTimeMilliseconds();
		List<EntityAvailableUser> list = _availableUsers.Values.Where((EntityAvailableUser u) => u.LastLoginTime < expirationThreshold).ToList();
		if (list.Count != 0)
		{
			await Task.WhenAll(list.Select((EntityAvailableUser user) => UpdateExpiredUserAsync(user, launcher)));
		}
	}

	private static async Task UpdateExpiredUserAsync(EntityAvailableUser expiredUser, WPFLauncher launcher)
	{
		try
		{
			EntityAuthenticationUpdate entityAuthenticationUpdate = await launcher.AuthenticationUpdateAsync(expiredUser.UserId, expiredUser.AccessToken);
            if (entityAuthenticationUpdate == null || entityAuthenticationUpdate.Token == null)
            {
                Log.Error("更新用户 {UserId} 的令牌失败", expiredUser.UserId);
                return;
            }
			expiredUser.AccessToken = entityAuthenticationUpdate.Token;
			expiredUser.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Log.Information("用户 {UserId} 的令牌已成功更新", expiredUser.UserId);
		}
		catch (Exception exception)
		{
            Log.Error(exception, "更新用户 {UserId} 时发生错误", expiredUser.UserId);
		}
	}

	public List<EntityUser> GetUsersNoDetails()
	{
		return _users.Values.Select((EntityUser u) => new EntityUser
		{
			UserId = u.UserId,
			Authorized = u.Authorized,
			AutoLogin = false,
			Channel = u.Channel,
			Type = u.Type,
			Details = "",
			Platform = u.Platform,
			Alias = u.Alias
		}).ToList();
	}

	public EntityUser? GetUserByEntityId(string entityId)
	{
		if (!_users.TryGetValue(entityId, out EntityUser value))
		{
			return null;
		}
		return value;
	}

    public EntityAvailableUser? GetLastAvailableUser()
    {
        return _availableUsers.Values.OrderBy(u => u.LastLoginTime).LastOrDefault();
    }

	public void AddUserToMaintain(EntityAuthenticationOtp authenticationOtp)
	{
		ArgumentNullException.ThrowIfNull(authenticationOtp, "authenticationOtp");
		EntityAvailableUser addValue = new EntityAvailableUser
		{
			UserId = authenticationOtp.EntityId,
			AccessToken = authenticationOtp.Token,
			LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		_availableUsers.AddOrUpdate(authenticationOtp.EntityId, addValue, delegate(string _, EntityAvailableUser existing)
		{
			existing.AccessToken = authenticationOtp.Token;
			existing.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			return existing;
		});
	}

	public void AddUser(EntityUser entityUser, bool saveToDisk = true)
	{
		ArgumentNullException.ThrowIfNull(entityUser, "entityUser");
		_users.AddOrUpdate(entityUser.UserId, entityUser, delegate(string _, EntityUser existing)
		{
			existing.Authorized = true;
			return existing;
		});
        if (saveToDisk)
        {
        }
	}

	public void RemoveUser(string entityId)
	{
        if (_users.TryRemove(entityId, out EntityUser _))
        {
        }
	}

	public void RemoveAvailableUser(string entityId)
	{
		_availableUsers.TryRemove(entityId, out EntityAvailableUser _);
        if (_users.TryGetValue(entityId, out EntityUser value2))
        {
            value2.Authorized = false;
        }
	}

    public async Task SaveUsersToDiskAsync()
    {
        await Task.CompletedTask;
        Log.Information("已禁用用户的本地保存");
    }
}
