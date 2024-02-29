using NBitcoin.RPC;
using System.Collections.Concurrent;

namespace DogiHubIndexer.Factories
{
    public class RPCClientPoolFactory
    {
        private readonly ConcurrentQueue<RPCClient> _clients;
        private readonly int _maxClients;

        public RPCClientPoolFactory(string authenticationString, string hostOrUri, int maxClients = 10)
        {
            _maxClients = maxClients;
            _clients = new ConcurrentQueue<RPCClient>();

            var network = NBitcoin.Altcoins.Dogecoin.Instance.Mainnet;

            for (int i = 0; i < _maxClients; i++)
            {
                var client = new RPCClient(authenticationString, hostOrUri, network);
                _clients.Enqueue(client);
            }
        }

        public async Task<T> UseClientAsync<T>(Func<RPCClient, Task<T>> func)
        {
            RPCClient client;

            while (!_clients.TryDequeue(out client!))
            {
                await Task.Delay(100);
            }

            try
            {
                return await func(client);
            }
            finally
            {
                _clients.Enqueue(client);
            }
        }

        public async Task UseClientAsync(Func<RPCClient, Task> func)
        {
            RPCClient client;

            while (!_clients.TryDequeue(out client!))
            {
                await Task.Delay(100);
            }

            try
            {
                await func(client);
            }
            finally
            {
                _clients.Enqueue(client);
            }
        }

        public async Task<RPCClient> GetClientAsync()
        {
            RPCClient client;

            while (!_clients.TryDequeue(out client!))
            {
                await Task.Delay(100);
            }

            return client;
        }

        public void ReturnClient(RPCClient client)
        {
            _clients.Enqueue(client);
        }
    }
}
