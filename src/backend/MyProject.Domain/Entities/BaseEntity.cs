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
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    public Guid? CreatedBy { get; private init; }

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who last updated the entity.
    /// </summary>
    public Guid? UpdatedBy { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the date and time when the entity was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who soft-deleted the entity.
    /// </summary>
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// Protected constructor for EF Core.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Soft-deletes the entity, marking it as deleted.
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
    }

    /// <summary>
    /// Restores a soft-deleted entity, marking it as not deleted.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
