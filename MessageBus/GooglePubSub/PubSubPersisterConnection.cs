namespace GooglePubSub
{
    public sealed class PubSubPersisterConnection
        : IPubSubPersisterConnection
    {
        bool _disposed;

        public PubSubPersisterConnection()
        {
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
        }
    }
}
