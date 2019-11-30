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
                "Handle",
                (items, ct) => mySql.EnqueueAsync(items.Select(x => x.Message.Payload), ct),
                maxBatchSize: 100,
                onSuccess: x => x.Next(remover),
                onFailure: (x, e) => { if (x.Message.ReceivedCount < 10) x.Close(Result.Error(e)); else x.Next(archiver, Result.Error(e)); });
            archiver = new ProcessorService<MySqlMessage>(
                "Archive",
                (items, ct) => mySql.ArchiveAsync(items.Select(x => x.Message), ct),
                concurrency: 8,
                maxBatchSize: 10,
                onSuccess: x => x.Next(remover));
            remover = new ProcessorService<MySqlMessage>(
                "Remove",
                (items, ct) => mySql.RemoveAsync(items.Select(x => x.Message), ct),
                maxBatchSize: 100,
                maxBatchDelay: TimeSpan.FromSeconds(10.0));

            return new QueueService<MySqlMessage>(receiver, handler, archiver, remover);
        }
    }
}
