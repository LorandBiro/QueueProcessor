using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using QueueProcessor.Logging;
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
            MySqlQueue mySql = new MySqlQueue(new TelemetryClient(TelemetryConfiguration.Active), "Server=db;Port=3306;Database=queue;Uid=root;Pwd=root;");
            await mySql.InitializeAsync();

            QueueService<MySqlMessage> mySqlToSqs = CreateMySqlToSqs(mySql);

            mySqlToSqs.Start();
            await Task.Delay(Timeout.InfiniteTimeSpan);
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
                new DebugLogger<MySqlMessage>(),
                receiver,
                _ => handler,
                handler,
                archiver,
                remover);
        }
    }
}
