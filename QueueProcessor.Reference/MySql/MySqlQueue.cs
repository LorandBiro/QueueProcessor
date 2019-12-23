using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using MySql.Data.MySqlClient;
using QueueProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Reference.MySql
{
    public sealed class MySqlQueue
    {
        private readonly TelemetryClient telemetryClient;
        private readonly string connectionString;

        public MySqlQueue(TelemetryClient telemetryClient, string connectionString)
        {
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task InitializeAsync()
        {
            string sql = @"CREATE TABLE IF NOT EXISTS queue
(
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    created_at DATETIME NOT NULL,
    lock_timeout DATETIME NOT NULL,
    lock_handle INT NULL,
    payload VARCHAR(8192) NOT NULL,
    lock_count tinyint unsigned not null default 0,
    INDEX queue_lock_handle_index (lock_handle),
    INDEX queue_lock_timeout_index (lock_timeout)
);

CREATE TABLE IF NOT EXISTS dead_letter
(
    id BIGINT UNSIGNED NOT NULL PRIMARY KEY,
    created_at DATETIME NOT NULL,
    died_at DATETIME NOT NULL,
    payload VARCHAR(8192) NOT NULL
);";

            using MySqlConnection connection = new MySqlConnection(this.connectionString);
            using IOperationHolder<DependencyTelemetry> operation = this.telemetryClient.StartOperation(new DependencyTelemetry("MySQL", connection.DataSource, "Initialize", sql));
            try
            {
                await connection.ExecuteAsync(sql);
            }
            catch (Exception e)
            {
                if (e is MySqlException mySqlException)
                {
                    operation.Telemetry.ResultCode = mySqlException.Number.ToString();
                }

                operation.Telemetry.Success = false;
                throw;
            }
        }

        public async Task EnqueueAsync(IEnumerable<string> messages, CancellationToken cancellationToken)
        {
            string sql = "INSERT INTO queue (created_at, lock_timeout, payload) VALUES " + string.Join(", ", messages.Select(x => $"(UTC_TIMESTAMP(), UTC_TIMESTAMP(), '{x}')")) + ";";

            using MySqlConnection connection = new MySqlConnection(this.connectionString);
            using IOperationHolder<DependencyTelemetry> operation = this.telemetryClient.StartOperation(new DependencyTelemetry("MySQL", connection.DataSource, "Archive", sql));
            try
            {
                await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            }
            catch (Exception e)
            {
                if (e is MySqlException mySqlException)
                {
                    operation.Telemetry.ResultCode = mySqlException.Number.ToString();
                }

                operation.Telemetry.Success = false;
                throw;
            }
        }

        public async Task<IReadOnlyCollection<MySqlMessage>> ReceiveAsync(int limit, int lockTimeoutInSeconds, CancellationToken cancellationToken)
        {
            int lockHandle = ThreadLocalRandom.Next();
            string sql = $@"
    UPDATE queue
    SET lock_timeout = DATE_ADD(UTC_TIMESTAMP(), INTERVAL {lockTimeoutInSeconds} SECOND),
        lock_count = lock_count + 1,
        lock_handle = {lockHandle}
    WHERE lock_timeout <= UTC_TIMESTAMP()
    LIMIT {limit};

    SELECT     id AS Id, payload AS Payload, created_at AS CreatedAt, lock_count as LockCount
    FROM       queue
    WHERE      lock_handle = {lockHandle}";

            using MySqlConnection connection = new MySqlConnection(this.connectionString);
            using IOperationHolder<DependencyTelemetry> operation = this.telemetryClient.StartOperation(new DependencyTelemetry("MySQL", connection.DataSource, "Receive", sql));
            try
            {
                DateTime messageLockTimeout = DateTime.UtcNow.AddSeconds(lockTimeoutInSeconds);
                IEnumerable<MessageDto> messages = await connection.QueryAsync<MessageDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
                return messages.Select(m => new MySqlMessage(m.Id, m.Payload, DateTime.SpecifyKind(m.CreatedAt, DateTimeKind.Utc), messageLockTimeout, m.LockCount)).ToList();
            }
            catch (Exception e)
            {
                if (e is MySqlException mySqlException)
                {
                    operation.Telemetry.ResultCode = mySqlException.Number.ToString();
                }

                operation.Telemetry.Success = false;
                throw;
            }
        }

        public async Task ArchiveAsync(IEnumerable<MySqlMessage> messages, CancellationToken cancellationToken)
        {
            string sql = $@"INSERT IGNORE INTO dead_letter (id, created_at, died_at, payload)
    SELECT id, created_at, UTC_TIMESTAMP(), payload
    FROM queue
    WHERE id in ({string.Join(", ", messages.Select(x => x.Id))});";

            using MySqlConnection connection = new MySqlConnection(this.connectionString);
            using IOperationHolder<DependencyTelemetry> operation = this.telemetryClient.StartOperation(new DependencyTelemetry("MySQL", connection.DataSource, "Archive", sql));
            try
            {
                await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            }
            catch (Exception e)
            {
                if (e is MySqlException mySqlException)
                {
                    operation.Telemetry.ResultCode = mySqlException.Number.ToString();
                }

                operation.Telemetry.Success = false;
                throw;
            }
        }

        public async Task<DateTime> RemoveAsync(IEnumerable<MySqlMessage> messages, CancellationToken cancellationToken)
        {
            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            string sql = $@"DELETE FROM queue WHERE id in ({string.Join(", ", messages.Select(x => x.Id))});
SELECT UTC_TIMESTAMP();";

            using MySqlConnection connection = new MySqlConnection(this.connectionString);
            using IOperationHolder<DependencyTelemetry> operation = this.telemetryClient.StartOperation(new DependencyTelemetry("MySQL", connection.DataSource, "Remove", sql));
            try
            {
                return await connection.ExecuteScalarAsync<DateTime>(new CommandDefinition(sql, cancellationToken: cancellationToken));
            }
            catch (Exception e)
            {
                if (e is MySqlException mySqlException)
                {
                    operation.Telemetry.ResultCode = mySqlException.Number.ToString();
                }

                operation.Telemetry.Success = false;
                throw;
            }
        }

        private class MessageDto
        {
            public long Id { get; set; }

            public string Payload { get; set; }

            public DateTime CreatedAt { get; set; }

            public int LockCount { get; set; }
        }
    }
}
