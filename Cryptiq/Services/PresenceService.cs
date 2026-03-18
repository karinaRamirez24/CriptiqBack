using StackExchange.Redis;

public class PresenceService
{
    private readonly IDatabase _db;
    private static readonly TimeSpan _ttl = TimeSpan.FromHours(1);

    public PresenceService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // Al conectarse: guarda las dos direcciones
    public async Task RegisterConnectionAsync(Guid userId, string connectionId)
    {
        var batch = _db.CreateBatch();
        batch.StringSetAsync($"user:{userId}:connection", connectionId, _ttl);
        batch.StringSetAsync($"conn:{connectionId}:userId", userId.ToString(), _ttl);
        batch.StringSetAsync($"user:{userId}:online", "1", TimeSpan.FromMinutes(5));
        batch.Execute();
        await Task.CompletedTask;
    }

    // Al desconectarse: limpia todo
    public async Task RemoveConnectionAsync(Guid userId)
    {
        var connId = await _db.StringGetAsync($"user:{userId}:connection");

        var batch = _db.CreateBatch();
        batch.KeyDeleteAsync($"user:{userId}:connection");
        batch.KeyDeleteAsync($"user:{userId}:online");
        if (!connId.IsNullOrEmpty)
            batch.KeyDeleteAsync($"conn:{connId}:userId");
        batch.Execute();
        await Task.CompletedTask;
    }

    // Obtener connectionId de un usuario (para enviarle mensajes)
    public async Task<string?> GetConnectionAsync(Guid userId)
    {
        var val = await _db.StringGetAsync($"user:{userId}:connection");
        return val.IsNullOrEmpty ? null : val.ToString();
    }

    // Obtener userId desde connectionId (para saber quién envía)
    public async Task<string?> GetUserIdByConnectionAsync(string connectionId)
    {
        var val = await _db.StringGetAsync($"conn:{connectionId}:userId");
        return val.IsNullOrEmpty ? null : val.ToString();
    }

    // Saber si está online
    public async Task<bool> IsUserOnlineAsync(Guid userId)
    {
        return await _db.KeyExistsAsync($"user:{userId}:online");
    }

    // Renovar presencia (llamar desde heartbeat o cada mensaje)
    public async Task RefreshPresenceAsync(Guid userId)
    {
        await _db.KeyExpireAsync($"user:{userId}:online", TimeSpan.FromMinutes(5));
        await _db.KeyExpireAsync($"user:{userId}:connection", _ttl);
    }
}