namespace Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }

        public void MarkUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (IsDeleted)
                throw new InvalidOperationException("Entity is already deleted.");
            
            IsDeleted = true;
            MarkUpdated();
        }

        public void Restore()
        {
            if (!IsDeleted)
                throw new InvalidOperationException("Entity is not deleted.");
            
            IsDeleted = false;
            MarkUpdated();
        }
    }
}