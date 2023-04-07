namespace RawClient.Brokers;

public interface IBrokerRepository
{
    Task AddAsync(Broker broker);
    Task<Broker?> GetAsync(Guid brokerId);
}

class InMemoryBrokerRepository : IBrokerRepository
{
    private readonly ICollection<Broker> _brokers = new List<Broker>();
    
    public Task AddAsync(Broker broker)
    {
        _brokers.Add(broker);
        return Task.CompletedTask;
    }

    public Task<Broker?> GetAsync(Guid brokerId)
    {
        var broker = _brokers.SingleOrDefault(x => x.Id == brokerId);
        return Task.FromResult(broker);
    }
}
