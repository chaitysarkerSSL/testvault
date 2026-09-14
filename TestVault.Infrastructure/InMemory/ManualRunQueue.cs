using System.Threading.Channels;
using TestVault.Application.Interfaces;

namespace TestVault.Infrastructure.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IManualRunQueue"/>, backed by an
/// unbounded <see cref="Channel{T}"/>. Unbounded is fine here: only one
/// manual run can ever be active at a time (enforced by
/// IManualRunTracker.TryStart before anything is enqueued), so this never
/// holds more than a single pending item in practice.
/// </summary>
public class ManualRunQueue : IManualRunQueue
{
    private readonly Channel<ManualRunJob> _channel = Channel.CreateUnbounded<ManualRunJob>();

    public void Enqueue(ManualRunJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<ManualRunJob> DequeueAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
