namespace EventArch.Domain.Common;

/// <summary>
/// Base type for objects defined by their identity rather than their attributes.
/// Two entities are equal when they have the same type and the same <see cref="Id"/>.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    // Required by ORMs that materialize objects through a parameterless constructor.
    protected Entity()
    {
    }

    public Guid Id { get; private init; }

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
