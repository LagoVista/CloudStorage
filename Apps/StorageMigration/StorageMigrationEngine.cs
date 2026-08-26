namespace LagoVista.StorageMigration;

public sealed class StorageMigrationEngine
{
    private readonly AzureTableMigrationSource _source;
    private readonly AzureTableRecordMapper _mapper;
    private readonly IActivityRecordMigrationWriter _writer;
    private readonly IMigrationStateStore _stateStore;

    public StorageMigrationEngine(AzureTableMigrationSource source, AzureTableRecordMapper mapper, IActivityRecordMigrationWriter writer, IMigrationStateStore stateStore)
    {
        _source = source; _mapper = mapper; _writer = writer; _stateStore = stateStore;
    }

    public async Task<MigrationRunState> ExecuteAsync(MigrationDefinition definition, string definitionSha256, bool catchUp = false, int? maxRecords = null, CancellationToken cancellationToken = default)
    {
        if (maxRecords.HasValue && maxRecords.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRecords), "Maximum records must be greater than zero.");

        var state = await _stateStore.GetAsync(definition.Key, cancellationToken).ConfigureAwait(false) ?? NewState(definition.Key, definitionSha256);
        if (!String.Equals(state.DefinitionSha256, definitionSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Migration {definition.Key} was started with definition {state.DefinitionSha256}, but current definition is {definitionSha256}.");
        var completed = String.Equals(state.State, "Completed", StringComparison.OrdinalIgnoreCase);
        if (completed && !catchUp) return state;
        if (catchUp && !completed) throw new InvalidOperationException($"Migration {definition.Key} is not completed. Resume before starting a catch-up replay.");
        if (catchUp) StartCatchUpPass(state);

        state.State = "Running";
        state.LastUpdatedDate = DateTime.UtcNow;
        await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
        await _writer.EnsureSchemaAsync(definition, cancellationToken).ConfigureAwait(false);

        var processedThisRun = 0;
        foreach (var table in await _source.ResolveTablesAsync(definition, cancellationToken).ConfigureAwait(false))
        {
            if (!String.IsNullOrWhiteSpace(state.CurrentTable))
            {
                var cmp = StringComparer.OrdinalIgnoreCase.Compare(table, state.CurrentTable);
                if (cmp < 0) continue;
                if (cmp == 0 && state.HeadPartitionKey == null && state.HeadRowKey == null) continue;
            }

            var resume = String.Equals(table, state.CurrentTable, StringComparison.OrdinalIgnoreCase);
            var batch = new List<IReadOnlyDictionary<string, object?>>(100);
            string? headPartition = null, headRow = null;
            await foreach (var row in _source.ReadAsync(table, resume ? state.HeadPartitionKey : null, resume ? state.HeadRowKey : null, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                state.RecordsRead++;
                processedThisRun++;
                try { batch.Add(_mapper.Map(definition, table, row)); headPartition = row.PartitionKey; headRow = row.RowKey; }
                catch { state.RecordsFailed++; state.LastUpdatedDate = DateTime.UtcNow; await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false); throw; }

                var limitReached = maxRecords.HasValue && processedThisRun >= maxRecords.Value;
                if (batch.Count >= 100 || limitReached)
                    await FlushAsync(definition, state, table, batch, headPartition, headRow, cancellationToken).ConfigureAwait(false);

                if (limitReached)
                    return state;
            }

            if (batch.Count > 0) await FlushAsync(definition, state, table, batch, headPartition, headRow, cancellationToken).ConfigureAwait(false);
            state.CurrentTable = table; state.HeadPartitionKey = null; state.HeadRowKey = null; state.LastUpdatedDate = DateTime.UtcNow;
            await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
        }

        state.State = "Completed"; state.CompletedDate = DateTime.UtcNow; state.LastUpdatedDate = state.CompletedDate.Value;
        await _stateStore.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
        return state;
    }

    private async Task FlushAsync(MigrationDefinition definition, MigrationRunState state, string table, List<IReadOnlyDictionary<string, object?>> batch, string? headPartition, string? headRow, CancellationToken ct)
    {
        await _writer.WriteBatchAsync(definition, batch, ct).ConfigureAwait(false);
        state.RecordsWritten += batch.Count; state.CurrentTable = table; state.HeadPartitionKey = headPartition; state.HeadRowKey = headRow; state.LastUpdatedDate = DateTime.UtcNow;
        await _stateStore.UpsertAsync(state, ct).ConfigureAwait(false); batch.Clear();
    }

    private static void StartCatchUpPass(MigrationRunState state)
    {
        state.PriorPassRecordsRead += state.RecordsRead; state.PriorPassRecordsWritten += state.RecordsWritten; state.PriorPassRecordsFailed += state.RecordsFailed;
        state.PassNumber = Math.Max(1, state.PassNumber) + 1; state.RecordsRead = state.RecordsWritten = state.RecordsFailed = 0;
        state.CurrentTable = state.HeadPartitionKey = state.HeadRowKey = null; state.CompletedDate = null;
    }

    private static MigrationRunState NewState(string key, string sha)
    {
        var now = DateTime.UtcNow;
        return new MigrationRunState { Id = Guid.NewGuid().ToString("N"), MigrationKey = key, DefinitionSha256 = sha, State = "NotStarted", PassNumber = 1, CreationDate = now, LastUpdatedDate = now };
    }
}
