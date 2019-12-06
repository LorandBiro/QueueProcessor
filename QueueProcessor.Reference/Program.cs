using QueueProcessor.MySql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QueueProcessor.Reference
{
    class Program
    {
        public static async Task Main()
        {
            MySqlQueue mySql = new MySqlQueue(null, "");
            await mySql.InitializeAsync();

            QueueService<MySqlMessage> mySqlToSqs = CreateMySqlToSqs(mySql);

            mySqlToSqs.Start();
        }

        private static QueueService<MySqlMessage> CreateMySqlToSqs(MySqlQueue mySql)
        {
            ProcessorService<MySqlMessage> handler = null, archiver = null, remover = null;

            ReceiverService<MySqlMessage> receiver = new ReceiverService<MySqlMessage>(
                ct => mySql.ReceiveAsync(100, 60, ct),
                _ => handler,
                concurrency: 4);
            handler = new ProcessorService<MySqlMessage>(
                "Handler",
                (items, ct) => mySql.EnqueueAsync(items.Select(x => x.Message.Payload), ct),
                maxBatchSize: 100,
                onSuccess: x => Op.TransferTo(remover),
                onFailure: x => x.Message.ReceivedCount < 10 ? Op.Close : Op.TransferTo(archiver));
            archiver = new ProcessorService<MySqlMessage>(
                "Archiver",
                (items, ct) => mySql.ArchiveAsync(items.Select(x => x.Message), ct),
                concurrency: 8,
                maxBatchSize: 10,
                onSuccess: x => Op.TransferTo(remover));
            remover = new ProcessorService<MySqlMessage>(
                "Remover",
                (items, ct) => mySql.RemoveAsync(items.Select(x => x.Message), ct),
                maxBatchSize: 100,
                maxBatchDelay: TimeSpan.FromSeconds(10.0));

            return new QueueService<MySqlMessage>(receiver, handler, archiver, remover);
        }
    }
}
