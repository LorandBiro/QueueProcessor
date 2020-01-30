using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using QueueProcessor.Processing;
using QueueProcessor.Receiving;
using QueueProcessor.Reference.MySql;
using QueueProcessor.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Reference
{
    class Program
    {
        public static async Task Main()
        {
            QueueService<long> queue = CreateUserIdInsertQueue();
            queue.Start();
            while (true)
            {
                for (int i = 0; i < 10; i++)
                {
                    queue.Enqueue(ThreadLocalRandom.Next());
                    await Task.Delay(100);
                }

                Console.WriteLine(queue.Count);
            }

            return;




            MySqlQueue mySql = new MySqlQueue(new TelemetryClient(TelemetryConfiguration.Active), "Server=db;Port=3306;Database=queue;Uid=root;Pwd=root;");
            await mySql.InitializeAsync();

            QueueService<MySqlMessage> mySqlToSqs = CreateMySqlToSqs(mySql);

            mySqlToSqs.Start();
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }

        private static QueueService<long> CreateUserIdInsertQueue()
        {
            Processor<long> handler = new Processor<long>("BatchInsert", (jobs, ct) => ThreadLocalRandom.NextDouble() < 0.9 ? Task.Delay(100) : Task.FromException(new Exception()), maxBatchSize: 1000, maxBatchDelay: TimeSpan.FromSeconds(1.0), retry: _ => true);
            return new QueueService<long>(null, x => handler, handler);
        }

        private static QueueService<MySqlMessage> CreateMySqlToSqs(MySqlQueue mySql)
        {
            Processor<MySqlMessage> handler = null, archiver = null, remover = null;

            Receiver<MySqlMessage> receiver = new Receiver<MySqlMessage>(
                "MySqlToSqsReceiver",
                ct => mySql.ReceiveAsync(5, 60, ct),
                new ReceiverStrategy(),
                concurrency: 4);
            handler = new Processor<MySqlMessage>(
                "MySqlToSqsHandler",
                async (jobs, ct) =>
                {
                    await Task.Delay(ThreadLocalRandom.Next(1000));
                    string newPayload;
                    string[] parts = jobs[0].Message.Payload.Split('-');
                    if (parts.Length == 1)
                    {
                        newPayload = parts[0] + "-1";
                    }
                    else
                    {
                        newPayload = parts[0] + "-" + (int.Parse(parts[1]) + 1);
                    }
                    await mySql.EnqueueAsync(new[] { newPayload }, ct);
                },
                maxBatchSize: 1,
                onSuccess: x => remover,
                onFailure: x => x.Message.ReceivedCount >= 10 ? archiver : null);
            archiver = new Processor<MySqlMessage>(
                "MySqlToSqsArchiver",
                (jobs, ct) => mySql.ArchiveAsync(jobs.Select(x => x.Message), ct),
                concurrency: 8,
                maxBatchSize: 10,
                onSuccess: x => remover);
            remover = new Processor<MySqlMessage>(
                "MySqlToSqsRemover",
                (jobs, ct) => mySql.RemoveAsync(jobs.Select(x => x.Message), ct),
                maxBatchSize: 100,
                maxBatchDelay: TimeSpan.FromSeconds(10.0));

            return new QueueService<MySqlMessage>(
                receiver,
                _ => handler,
                handler,
                archiver,
                remover);
        }
    }
}
