using System.Threading.Channels;
using ProjectSift.Application.Models;

namespace ProjectSift.Service.BackgroundServices;

public sealed class BoundedBatchWorkQueue : IBatchWorkQueue
{
    private readonly Channel<BatchProcessingRequest> _queue;

    public BoundedBatchWorkQueue(int capacity)
    {
        _queue = Channel.CreateBounded<BatchProcessingRequest>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask<bool> TryEnqueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_queue.Writer.TryWrite(request));
    }

    public IAsyncEnumerable<BatchProcessingRequest> ReadAllAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAllAsync(cancellationToken);
}
