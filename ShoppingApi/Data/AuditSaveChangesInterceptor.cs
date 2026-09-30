using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Data;

public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    : SaveChangesInterceptor
{
    private readonly ConcurrentDictionary<Guid, SaveState> _saveStates = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        PrepareSave(eventData.Context);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await PrepareSaveAsync(eventData.Context, cancellationToken);
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        CompleteSave(eventData.Context);
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await CompleteSaveAsync(eventData.Context, cancellationToken);
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        FailSave(eventData.Context);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await FailSaveAsync(eventData.Context, cancellationToken);
    }

    private void PrepareSave(DbContext? context)
    {
        if (!TryCaptureChanges(context, out var state))
        {
            return;
        }

        if (context!.Database.IsRelational() && context.Database.CurrentTransaction is null)
        {
            state.Transaction = context.Database.BeginTransaction();
        }

        _saveStates[context.ContextId.InstanceId] = state;
        context.Set<Audit>().AddRange(state.Entries.Select(entry => entry.Audit));
    }

    private async Task PrepareSaveAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (!TryCaptureChanges(context, out var state))
        {
            return;
        }

        if (context!.Database.IsRelational() && context.Database.CurrentTransaction is null)
        {
            state.Transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        }

        _saveStates[context.ContextId.InstanceId] = state;
        context.Set<Audit>().AddRange(state.Entries.Select(entry => entry.Audit));
    }

    private bool TryCaptureChanges(DbContext? context, out SaveState state)
    {
        state = null!;
        if (context is null)
        {
            return false;
        }

        if (_saveStates.ContainsKey(context.ContextId.InstanceId))
        {
            return false;
        }

        context.ChangeTracker.DetectChanges();
        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is not Audit &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(CapturedEntry.Create)
            .ToList();
        if (entries.Count == 0)
        {
            return false;
        }

        var authenticatedUserId = httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);
        state = new SaveState(entries, string.IsNullOrWhiteSpace(authenticatedUserId)
            ? ResolveEntityUserId(entries)
            : authenticatedUserId);
        foreach (var entry in entries)
        {
            entry.Audit.UserId = state.UserId;
        }

        return true;
    }

    private void CompleteSave(DbContext? context)
    {
        if (context is null ||
            !_saveStates.TryGetValue(context.ContextId.InstanceId, out var state))
        {
            return;
        }

        try
        {
            FinalizeAuditRecords(context, state);
            state.Transaction?.Commit();
        }
        catch
        {
            state.Transaction?.Rollback();
            throw;
        }
        finally
        {
            state.Transaction?.Dispose();
            _saveStates.TryRemove(context.ContextId.InstanceId, out _);
        }
    }

    private async Task CompleteSaveAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null ||
            !_saveStates.TryGetValue(context.ContextId.InstanceId, out var state))
        {
            return;
        }

        try
        {
            await FinalizeAuditRecordsAsync(context, state, cancellationToken);
            if (state.Transaction is not null)
            {
                await state.Transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (state.Transaction is not null)
            {
                await state.Transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (state.Transaction is not null)
            {
                await state.Transaction.DisposeAsync();
            }

            _saveStates.TryRemove(context.ContextId.InstanceId, out _);
        }
    }

    private static void FinalizeAuditRecords(DbContext context, SaveState state)
    {
        foreach (var captured in state.Entries.Where(entry => entry.HasGeneratedKey))
        {
            captured.Audit.RecordId = GetRecordId(captured.Entry);
            captured.Audit.After = captured.CreateInsertChanges();
            captured.Audit.Before = "{}";
        }

        if (state.Entries.Any(entry => entry.HasGeneratedKey))
        {
            if (context.Database.IsRelational())
            {
                UpdateGeneratedAuditRecords(context, state);
            }
            else
            {
                context.SaveChanges();
            }
        }
    }

    private static async Task FinalizeAuditRecordsAsync(
        DbContext context,
        SaveState state,
        CancellationToken cancellationToken)
    {
        foreach (var captured in state.Entries.Where(entry => entry.HasGeneratedKey))
        {
            captured.Audit.RecordId = GetRecordId(captured.Entry);
            captured.Audit.After = captured.CreateInsertChanges();
            captured.Audit.Before = "{}";
        }

        if (!state.Entries.Any(entry => entry.HasGeneratedKey))
        {
            return;
        }

        if (context.Database.IsRelational())
        {
            await UpdateGeneratedAuditRecordsAsync(context, state, cancellationToken);
        }
        else
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void UpdateGeneratedAuditRecords(DbContext context, SaveState state)
    {
        foreach (var captured in state.Entries.Where(entry => entry.HasGeneratedKey))
        {
            context.Set<Audit>()
                .Where(audit => audit.Id == captured.Audit.Id)
                .ExecuteUpdate(update => update
                    .SetProperty(audit => audit.RecordId, captured.Audit.RecordId)
                    .SetProperty(audit => audit.After, captured.Audit.After));
        }
    }

    private static async Task UpdateGeneratedAuditRecordsAsync(
        DbContext context,
        SaveState state,
        CancellationToken cancellationToken)
    {
        foreach (var captured in state.Entries.Where(entry => entry.HasGeneratedKey))
        {
            await context.Set<Audit>()
                .Where(audit => audit.Id == captured.Audit.Id)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(audit => audit.RecordId, captured.Audit.RecordId)
                    .SetProperty(audit => audit.After, captured.Audit.After),
                    cancellationToken);
        }
    }

    private void FailSave(DbContext? context)
    {
        if (context is null ||
            !_saveStates.TryRemove(context.ContextId.InstanceId, out var state))
        {
            return;
        }

        state.Transaction?.Rollback();
        state.Transaction?.Dispose();
    }

    private async Task FailSaveAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null ||
            !_saveStates.TryRemove(context.ContextId.InstanceId, out var state))
        {
            return;
        }

        if (state.Transaction is not null)
        {
            await state.Transaction.RollbackAsync(cancellationToken);
            await state.Transaction.DisposeAsync();
        }
    }

    private static string? ResolveEntityUserId(IEnumerable<CapturedEntry> entries)
    {
        var userIds = entries
            .SelectMany(captured => captured.Entry.Properties)
            .Where(property => property.Metadata.Name is
                "CreatedByUserId" or "UpdatedByUserId" or "DeletedByUserId" or "UserId")
            .Select(property => property.CurrentValue as string)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (userIds.Count == 1)
        {
            return userIds[0];
        }

        var insertedUserId = entries
            .Select(captured => captured.Entry.Entity)
            .OfType<ApplicationUser>()
            .Select(user => user.Id)
            .FirstOrDefault(userId => !string.IsNullOrWhiteSpace(userId));
        return insertedUserId;
    }

    private static string GetRecordId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity '{entry.Metadata.Name}' has no primary key.");
        var values = primaryKey.Properties.ToDictionary(
            property => property.Name,
            property => entry.Property(property.Name).CurrentValue);

        return values.Count == 1
            ? Convert.ToString(values.Values.Single(), CultureInfo.InvariantCulture) ?? string.Empty
            : JsonSerializer.Serialize(values);
    }

    private static object? RedactSensitiveValue(string propertyName, object? value)
    {
        return propertyName is "PasswordHash" or "SecurityStamp" or "ConcurrencyStamp" or "TokenHash"
            ? value is null ? null : "[REDACTED]"
            : value;
    }

    private sealed class SaveState(List<CapturedEntry> entries, string? userId)
    {
        public List<CapturedEntry> Entries { get; } = entries;
        public string? UserId { get; } = userId;
        public IDbContextTransaction? Transaction { get; set; }
    }

    private sealed class CapturedEntry
    {
        private CapturedEntry(EntityEntry entry, string tableName, string operation, Audit audit)
        {
            Entry = entry;
            TableName = tableName;
            Operation = operation;
            Audit = audit;
            HasGeneratedKey = entry.State == EntityState.Added &&
                entry.Metadata.FindPrimaryKey()!.Properties.Any(property =>
                    entry.Property(property.Name).IsTemporary);
            Changes = operation switch
            {
                "Delete" => "{}",
                "Insert" => CreateInsertChanges(entry),
                _ => CreateModifiedChanges(entry)
            };
            Before = operation switch
            {
                "Insert" => "{}",
                "Delete" => CreateOriginalValues(entry.Properties),
                _ => CreateOriginalValues(entry.Properties.Where(property => property.IsModified))
            };
        }

        public EntityEntry Entry { get; }
        public string TableName { get; }
        public string Operation { get; }
        public Audit Audit { get; }
        public bool HasGeneratedKey { get; }
        private string Changes { get; }
        private string Before { get; }

        public string CreateInsertChanges()
        {
            return CreateInsertChanges(Entry);
        }

        public static CapturedEntry Create(EntityEntry entry)
        {
            var operation = entry.State switch
            {
                EntityState.Added => "Insert",
                EntityState.Deleted => "Delete",
                EntityState.Modified when IsSoftDelete(entry) => "SoftDelete",
                EntityState.Modified => "Update",
                _ => throw new InvalidOperationException("Unsupported audited entity state.")
            };
            var tableName = entry.Metadata.GetTableName()
                ?? throw new InvalidOperationException($"Entity '{entry.Metadata.Name}' has no table mapping.");
            var captured = new CapturedEntry(entry, tableName, operation, new Audit());
            captured.Audit.UserId = null;
            captured.Audit.TableName = tableName;
            captured.Audit.RecordId = captured.HasGeneratedKey ? string.Empty : GetRecordId(entry);
            captured.Audit.Operation = operation;
            captured.Audit.OperationDate = DateTimeOffset.UtcNow;
            captured.Audit.After = operation == "Insert"
                ? captured.CreateInsertChanges()
                : captured.Changes;
            captured.Audit.Before = captured.Before;
            return captured;
        }

        private static string CreateOriginalValues(IEnumerable<PropertyEntry> properties)
        {
            return JsonSerializer.Serialize(properties.ToDictionary(
                property => property.Metadata.Name,
                property => RedactSensitiveValue(property.Metadata.Name, property.OriginalValue)));
        }

        private static string CreateInsertChanges(EntityEntry entry)
        {
            return JsonSerializer.Serialize(entry.Properties.ToDictionary(
                property => property.Metadata.Name,
                property => RedactSensitiveValue(property.Metadata.Name, property.CurrentValue)));
        }

        private static string CreateModifiedChanges(EntityEntry entry)
        {
            return JsonSerializer.Serialize(entry.Properties
                .Where(property => property.IsModified)
                .ToDictionary(
                    property => property.Metadata.Name,
                    property => RedactSensitiveValue(property.Metadata.Name, property.CurrentValue)));
        }

        private static bool IsSoftDelete(EntityEntry entry)
        {
            var deletedAt = entry.Properties.FirstOrDefault(property =>
                property.Metadata.Name == nameof(ShoppingList.DeletedAt));

            return deletedAt is { IsModified: true, CurrentValue: not null };
        }
    }
}
