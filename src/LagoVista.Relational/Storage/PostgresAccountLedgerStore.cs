using LagoVista;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Relational.Storage
{
    [CriticalCoverage]
    public sealed class PostgresAccountLedgerStore<TRecord> : IAccountLedgerStore<TRecord>
        where TRecord : class, IAccountLedgerRecord
    {
        private readonly IAccountLedgerStorageSettings _settings;
        private readonly string _schema;
        private readonly string _table;
        private readonly SemaphoreSlim _schemaLock = new SemaphoreSlim(1, 1);
        private volatile bool _schemaReady;

        public PostgresAccountLedgerStore(IAccountLedgerStorageSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (String.IsNullOrWhiteSpace(settings.SchemaName)) throw new ArgumentException("Account ledger schema name is required.", nameof(settings));

            _schema = QuoteIdentifier(settings.SchemaName);
            _table = $"{_schema}.{QuoteIdentifier("account_ledger_entries")}";
        }

        public async Task<AccountLedgerEntry<TRecord>> AddTransactionAsync(TRecord transaction, CancellationToken cancellationToken = default)
        {
            ValidateTransaction(transaction);
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var ledgerKey = LedgerKey(transaction.OrganizationId, transaction.AccountId, transaction.TransactionType);
            await AcquireLedgerLockAsync(connection, dbTransaction, ledgerKey, cancellationToken).ConfigureAwait(false);

            var prior = await GetLatestStateAsync(connection, dbTransaction, transaction.OrganizationId, transaction.AccountId, transaction.TransactionType, cancellationToken).ConfigureAwait(false);
            var sequence = prior.Sequence + 1;
            var balance = prior.Balance + transaction.CreditAmount.GetValueOrDefault() - transaction.DebitAmount.GetValueOrDefault();
            var previousHash = prior.IntegrityHash ?? String.Empty;
            var integrityHash = ComputeIntegrityHash(transaction, sequence, balance, previousHash);
            var creationDate = NormalizeUtc(transaction.CreationDate);

            var sql = $@"
INSERT INTO {_table} (
    id, organization_id, organization, account_id, account, transaction_type,
    sequence, creation_date, credit_amount, debit_amount, balance,
    previous_integrity_hash, integrity_hash, record_json)
VALUES (
    @id, @organization_id, @organization, @account_id, @account, @transaction_type,
    @sequence, @creation_date, @credit_amount, @debit_amount, @balance,
    @previous_integrity_hash, @integrity_hash, @record_json);";

            await using var command = new NpgsqlCommand(sql, connection, dbTransaction);
            command.Parameters.AddWithValue("id", transaction.Id);
            command.Parameters.AddWithValue("organization_id", transaction.OrganizationId);
            command.Parameters.AddWithValue("organization", transaction.Organization);
            command.Parameters.AddWithValue("account_id", transaction.AccountId);
            command.Parameters.AddWithValue("account", transaction.Account);
            command.Parameters.AddWithValue("transaction_type", transaction.TransactionType);
            command.Parameters.AddWithValue("sequence", sequence);
            command.Parameters.AddWithValue("creation_date", creationDate);
            command.Parameters.AddWithValue("credit_amount", NpgsqlDbType.Numeric, (object)transaction.CreditAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("debit_amount", NpgsqlDbType.Numeric, (object)transaction.DebitAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("balance", balance);
            command.Parameters.AddWithValue("previous_integrity_hash", previousHash);
            command.Parameters.AddWithValue("integrity_hash", integrityHash);
            command.Parameters.AddWithValue("record_json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(transaction));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new AccountLedgerEntry<TRecord>(transaction, balance, integrityHash);
        }

        public async Task<decimal> GetBalanceAsync(string organizationId, string accountId, string transactionType, CancellationToken cancellationToken = default)
        {
            ValidateLedgerIdentity(organizationId, accountId, transactionType);
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var sql = $@"
SELECT balance
FROM {_table}
WHERE organization_id = @organization_id
  AND account_id = @account_id
  AND transaction_type = @transaction_type
ORDER BY sequence DESC
LIMIT 1;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("organization_id", organizationId);
            command.Parameters.AddWithValue("account_id", accountId);
            command.Parameters.AddWithValue("transaction_type", transactionType);
            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public async Task<StoragePageResult<AccountLedgerEntry<TRecord>>> QueryAsync(AccountLedgerQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            ValidateLedgerIdentity(query.OrganizationId, query.AccountId, query.TransactionType);
            if (query.StartDate.HasValue && query.EndDate.HasValue && query.StartDate.Value > query.EndDate.Value) throw new ArgumentException("Ledger query start date cannot be after end date.", nameof(query));
            cancellationToken.ThrowIfCancellationRequested();

            var page = query.Page ?? new StoragePageRequest();
            var cursor = DecodeCursor(page.ContinuationToken);
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            var where = new StringBuilder(@"organization_id = @organization_id
  AND account_id = @account_id
  AND transaction_type = @transaction_type");
            if (query.StartDate.HasValue) where.Append(" AND creation_date >= @start_date");
            if (query.EndDate.HasValue) where.Append(" AND creation_date <= @end_date");
            if (cursor.HasValue) where.Append(" AND sequence < @cursor_sequence");

            var sql = $@"
SELECT record_json::text, balance, integrity_hash, sequence
FROM {_table}
WHERE {where}
ORDER BY sequence DESC
LIMIT @limit;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("organization_id", query.OrganizationId);
            command.Parameters.AddWithValue("account_id", query.AccountId);
            command.Parameters.AddWithValue("transaction_type", query.TransactionType);
            if (query.StartDate.HasValue) command.Parameters.AddWithValue("start_date", NormalizeUtc(query.StartDate.Value));
            if (query.EndDate.HasValue) command.Parameters.AddWithValue("end_date", NormalizeUtc(query.EndDate.Value));
            if (cursor.HasValue) command.Parameters.AddWithValue("cursor_sequence", cursor.Value);
            command.Parameters.AddWithValue("limit", page.PageSize + 1);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var materialized = new List<(AccountLedgerEntry<TRecord> Entry, long Sequence)>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var record = JsonSerializer.Deserialize<TRecord>(reader.GetString(0));
                if (record == null) throw new InvalidOperationException("Stored ledger transaction could not be deserialized.");

                materialized.Add((new AccountLedgerEntry<TRecord>(record, reader.GetDecimal(1), reader.GetString(2)), reader.GetInt64(3)));
            }

            var hasMore = materialized.Count > page.PageSize;
            var items = materialized.Take(page.PageSize).ToList();
            var continuationToken = hasMore && items.Count > 0 ? EncodeCursor(items[items.Count - 1].Sequence) : null;
            return new StoragePageResult<AccountLedgerEntry<TRecord>>(items.Select(item => item.Entry), continuationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            var connection = new NpgsqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }

        private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
        {
            if (_schemaReady) return;

            await _schemaLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_schemaReady) return;

                await using var connection = new NpgsqlConnection(BuildConnectionString());
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var sql = $@"
CREATE SCHEMA IF NOT EXISTS {_schema};
CREATE TABLE IF NOT EXISTS {_table} (
    id text PRIMARY KEY,
    organization_id text NOT NULL,
    organization text NOT NULL,
    account_id text NOT NULL,
    account text NOT NULL,
    transaction_type text NOT NULL,
    sequence bigint NOT NULL,
    creation_date timestamptz NOT NULL,
    credit_amount numeric NULL,
    debit_amount numeric NULL,
    balance numeric NOT NULL,
    previous_integrity_hash text NOT NULL,
    integrity_hash text NOT NULL,
    record_json jsonb NOT NULL,
    CONSTRAINT ck_account_ledger_single_amount CHECK ((credit_amount IS NULL) <> (debit_amount IS NULL)),
    CONSTRAINT ck_account_ledger_credit_positive CHECK (credit_amount IS NULL OR credit_amount > 0),
    CONSTRAINT ck_account_ledger_debit_positive CHECK (debit_amount IS NULL OR debit_amount > 0),
    CONSTRAINT uq_account_ledger_sequence UNIQUE (organization_id, account_id, transaction_type, sequence)
);
CREATE INDEX IF NOT EXISTS ix_account_ledger_lookup ON {_table} (organization_id, account_id, transaction_type, sequence DESC);
CREATE INDEX IF NOT EXISTS ix_account_ledger_date ON {_table} (organization_id, account_id, transaction_type, creation_date DESC);";

                await using var command = new NpgsqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                _schemaReady = true;
            }
            finally
            {
                _schemaLock.Release();
            }
        }

        private async Task AcquireLedgerLockAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string ledgerKey, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@ledger_key, 0));", connection, transaction);
            command.Parameters.AddWithValue("ledger_key", ledgerKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<(long Sequence, decimal Balance, string IntegrityHash)> GetLatestStateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string organizationId, string accountId, string transactionType, CancellationToken cancellationToken)
        {
            var sql = $@"
SELECT sequence, balance, integrity_hash
FROM {_table}
WHERE organization_id = @organization_id
  AND account_id = @account_id
  AND transaction_type = @transaction_type
ORDER BY sequence DESC
LIMIT 1;";

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("organization_id", organizationId);
            command.Parameters.AddWithValue("account_id", accountId);
            command.Parameters.AddWithValue("transaction_type", transactionType);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return (0, 0m, String.Empty);
            return (reader.GetInt64(0), reader.GetDecimal(1), reader.GetString(2));
        }

        private string BuildConnectionString()
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = _settings.HostName,
                Port = _settings.Port,
                Username = _settings.UserName,
                Password = _settings.Password,
                Timeout = 10,
                CommandTimeout = 30,
                Pooling = true
            }.ConnectionString;
        }

        private static void ValidateTransaction(TRecord transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (String.IsNullOrWhiteSpace(transaction.Id)) throw new ArgumentException("Transaction Id is required.", nameof(transaction));
            ValidateLedgerIdentity(transaction.OrganizationId, transaction.AccountId, transaction.TransactionType);
            if (String.IsNullOrWhiteSpace(transaction.Organization)) throw new ArgumentException("Transaction Organization is required.", nameof(transaction));
            if (String.IsNullOrWhiteSpace(transaction.Account)) throw new ArgumentException("Transaction Account is required.", nameof(transaction));

            var hasCredit = transaction.CreditAmount.HasValue;
            var hasDebit = transaction.DebitAmount.HasValue;
            if (hasCredit == hasDebit) throw new ArgumentException("Exactly one of CreditAmount or DebitAmount must be supplied.", nameof(transaction));
            if (hasCredit && transaction.CreditAmount.Value <= 0) throw new ArgumentOutOfRangeException(nameof(transaction), "CreditAmount must be greater than zero.");
            if (hasDebit && transaction.DebitAmount.Value <= 0) throw new ArgumentOutOfRangeException(nameof(transaction), "DebitAmount must be greater than zero.");
        }

        private static void ValidateLedgerIdentity(string organizationId, string accountId, string transactionType)
        {
            if (String.IsNullOrWhiteSpace(organizationId)) throw new ArgumentNullException(nameof(organizationId));
            if (String.IsNullOrWhiteSpace(accountId)) throw new ArgumentNullException(nameof(accountId));
            if (String.IsNullOrWhiteSpace(transactionType)) throw new ArgumentNullException(nameof(transactionType));
        }

        private static string ComputeIntegrityHash(TRecord transaction, long sequence, decimal balance, string previousHash)
        {
            var payload = String.Join("|", new[]
            {
                previousHash ?? String.Empty,
                transaction.OrganizationId,
                transaction.AccountId,
                transaction.TransactionType,
                sequence.ToString(CultureInfo.InvariantCulture),
                transaction.Id,
                NormalizeUtc(transaction.CreationDate).ToString("O", CultureInfo.InvariantCulture),
                transaction.CreditAmount?.ToString(CultureInfo.InvariantCulture) ?? String.Empty,
                transaction.DebitAmount?.ToString(CultureInfo.InvariantCulture) ?? String.Empty,
                balance.ToString(CultureInfo.InvariantCulture)
            });

            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private static string LedgerKey(string organizationId, string accountId, string transactionType) => $"{organizationId}\u001f{accountId}\u001f{transactionType}";

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static string EncodeCursor(long sequence) => Convert.ToBase64String(Encoding.UTF8.GetBytes(sequence.ToString(CultureInfo.InvariantCulture)));

        private static long? DecodeCursor(string continuationToken)
        {
            if (String.IsNullOrWhiteSpace(continuationToken)) return null;
            try
            {
                var text = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));
                if (!Int64.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) || sequence <= 0) throw new FormatException();
                return sequence;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("The account ledger continuation token is invalid.", nameof(continuationToken), ex);
            }
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (String.IsNullOrWhiteSpace(identifier)) throw new ArgumentNullException(nameof(identifier));
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }
    }
}
