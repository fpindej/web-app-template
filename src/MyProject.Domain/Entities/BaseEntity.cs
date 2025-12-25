namespace MyProject.Domain.Entities;

/// <summary>
/// Represents the base class for all entities in the domain, providing common properties and behavior.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; protected init; }

    /// <summary>
    /// Gets the date and time when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; private init; }

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the date and time when the entity was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// Protected constructor for EF Core.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class with the specified creation time.
    /// </summary>
    /// <param name="createdAt">The creation time.</param>
    protected BaseEntity(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Sets the last updated time for the entity.
    /// </summary>
    /// <param name="updatedAt">The new updated time.</param>
    protected void SetUpdatedAt(DateTime updatedAt)
    {
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Soft-deletes the entity, marking it as deleted and setting the deletion time.
    /// </summary>
    /// <param name="deletedAt">The time of deletion.</param>
    public void SoftDelete(DateTime deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = deletedAt;
        SetUpdatedAt(deletedAt);
    }

    /// <summary>
    /// Restores a soft-deleted entity, marking it as not deleted and clearing the deletion time.
    /// </summary>
    /// <param name="restoredAt">The time of restoration.</param>
    public void Restore(DateTime restoredAt)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAt = null;
        SetUpdatedAt(restoredAt);
    }
}
