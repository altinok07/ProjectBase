using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectBase.Core.Entities;
using System.Security.Claims;

namespace ProjectBase.Core.Data.Contexts;

/// <summary>
/// Base DbContext implementation with automatic audit trail and soft delete support.
/// </summary>
public abstract class BaseContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor) : DbContext(options)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates audit fields for all tracked entities and handles soft delete.
    /// </summary>
    private void UpdateAuditEntities()
    {
        var modifiedEntries = GetModifiedEntities();
        if (modifiedEntries.Count == 0)
        {
            return;
        }

        var currentUser = GetCurrentUser();
        var now = DateTime.Now;

        foreach (var entry in modifiedEntries)
        {
            UpdateEntityAuditFields(entry, currentUser, now);
        }
    }

    /// <summary>
    /// Gets all tracked entities that have been added, modified, or deleted.
    /// </summary>
    private List<EntityEntry<BaseEntity>> GetModifiedEntities()
    {
        return ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
    }

    /// <summary>
    /// Updates audit fields for a single entity based on its current state.
    /// </summary>
    private void UpdateEntityAuditFields(EntityEntry<BaseEntity> entry, string? currentUser, DateTime now)
    {
        var entity = entry.Entity;

        switch (entry.State)
        {
            case EntityState.Added:
                SetCreatedFields(entity, currentUser, now);
                SetUpdatedFields(entity, currentUser, now);
                break;

            case EntityState.Modified:
                PreventCreatedFieldsModification(entry);
                SetUpdatedFields(entity, currentUser, now);

                // If entity is being soft-deleted via "Modified" state (instead of "Deleted"),
                // ensure Deleted* audit fields are populated.
                if (entity.IsDeleted && entry.Property(e => e.IsDeleted).IsModified)
                {
                    entity.DeletedDate ??= now;
                    entity.DeletedBy ??= currentUser;

                    entry.Property(e => e.DeletedDate).IsModified = true;
                    entry.Property(e => e.DeletedBy).IsModified = true;
                }
                break;

            case EntityState.Deleted:
                HandleEntityDeletion(entry, entity, currentUser, now);
                break;
        }
    }

    /// <summary>
    /// Sets the created audit fields for a new entity.
    /// </summary>
    private static void SetCreatedFields(BaseEntity entity, string? currentUser, DateTime now)
    {
        entity.CreatedDate = now;
        entity.CreatedBy = currentUser;
    }

    /// <summary>
    /// Sets the updated audit fields for an entity.
    /// </summary>
    private static void SetUpdatedFields(BaseEntity entity, string? currentUser, DateTime now)
    {
        entity.UpdatedDate = now;
        entity.UpdatedBy = currentUser;
    }

    /// <summary>
    /// Prevents modification of CreatedBy and CreatedDate fields during updates.
    /// </summary>
    private static void PreventCreatedFieldsModification(EntityEntry<BaseEntity> entry)
    {
        entry.Property(e => e.CreatedBy).IsModified = false;
        entry.Property(e => e.CreatedDate).IsModified = false;
    }

    /// <summary>
    /// Handles entity deletion: performs soft delete unless entity implements IHardDelete.
    /// </summary>
    private void HandleEntityDeletion(EntityEntry<BaseEntity> entry, BaseEntity entity, string? currentUser, DateTime utcNow)
    {
        if (entity is IHardDelete)
        {
            // Allow physical deletion - EF Core will handle it
            return;
        }

        PerformSoftDelete(entry, entity, currentUser, utcNow);
    }

    /// <summary>
    /// Performs soft delete by setting IsDeleted flag and audit fields.
    /// </summary>
    private static void PerformSoftDelete(EntityEntry<BaseEntity> entry, BaseEntity entity, string? currentUser, DateTime utcNow)
    {
        // Convert physical delete request into an UPDATE (soft delete).
        // IMPORTANT: We must not keep EntityState.Deleted, otherwise EF may try to fix up
        // required relationships (e.g. set FK to null) and throw "association severed" exceptions.
        entry.State = EntityState.Modified;

        // Only update the soft-delete + audit fields (avoid updating every column).
        foreach (var p in entry.Properties)
            p.IsModified = false;

        entity.IsDeleted = true;
        entity.DeletedDate = utcNow;
        entity.DeletedBy = currentUser;
        SetUpdatedFields(entity, currentUser, utcNow);

        entry.Property(e => e.IsDeleted).IsModified = true;
        entry.Property(e => e.DeletedDate).IsModified = true;
        entry.Property(e => e.DeletedBy).IsModified = true;
        entry.Property(e => e.UpdatedDate).IsModified = true;
        entry.Property(e => e.UpdatedBy).IsModified = true;
    }

    /// <summary>
    /// Marks soft delete fields as modified so they will be saved to the database.
    /// </summary>
    private static void MarkDeleteFieldsAsModified(EntityEntry<BaseEntity> entry)
    {
        entry.Property(e => e.IsDeleted).IsModified = true;
        entry.Property(e => e.DeletedDate).IsModified = true;
        entry.Property(e => e.DeletedBy).IsModified = true;
    }

    /// <summary>
    /// Gets the current authenticated user's identifier from the HTTP context.
    /// </summary>
    /// <returns>The username or identifier if available, otherwise null.</returns>
    private string? GetCurrentUser()
    {
        var identity = _httpContextAccessor?.HttpContext?.User?.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return null;
        }

        return identity.FindFirst("UserName")?.Value
            ?? identity.FindFirst(ClaimTypes.Name)?.Value
            ?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

