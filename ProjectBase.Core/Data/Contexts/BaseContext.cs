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
        var utcNow = DateTime.UtcNow;

        foreach (var entry in modifiedEntries)
        {
            UpdateEntityAuditFields(entry, currentUser, utcNow);
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
    private void UpdateEntityAuditFields(EntityEntry<BaseEntity> entry, string? currentUser, DateTime utcNow)
    {
        var entity = entry.Entity;

        switch (entry.State)
        {
            case EntityState.Added:
                SetCreatedFields(entity, currentUser, utcNow);
                SetUpdatedFields(entity, currentUser, utcNow);
                break;

            case EntityState.Modified:
                PreventCreatedFieldsModification(entry);
                SetUpdatedFields(entity, currentUser, utcNow);
                break;

            case EntityState.Deleted:
                HandleEntityDeletion(entry, entity, currentUser, utcNow);
                break;
        }
    }

    /// <summary>
    /// Sets the created audit fields for a new entity.
    /// </summary>
    private static void SetCreatedFields(BaseEntity entity, string? currentUser, DateTime utcNow)
    {
        entity.CreatedDate = utcNow;
        entity.CreatedBy = currentUser;
    }

    /// <summary>
    /// Sets the updated audit fields for an entity.
    /// </summary>
    private static void SetUpdatedFields(BaseEntity entity, string? currentUser, DateTime utcNow)
    {
        entity.UpdatedDate = utcNow;
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
        // Convert to soft delete
        entry.State = EntityState.Unchanged;
        
        entity.IsDeleted = true;
        entity.DeletedDate = utcNow;
        entity.DeletedBy = currentUser;
        
        MarkDeleteFieldsAsModified(entry);
        SetUpdatedFields(entity, currentUser, utcNow);
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

