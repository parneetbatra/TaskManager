namespace TaskManager.Domain.Exceptions
{
    public sealed class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(Guid id)
            : base($"Entity with id '{id}' was not found.")
        {
        }
    }
}
