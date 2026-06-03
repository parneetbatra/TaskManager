namespace TaskManager.Application.Dtos.Responses
{
    public sealed class PagedResult<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public IEnumerable<T> Items { get; init; } = null!;
    }
}
