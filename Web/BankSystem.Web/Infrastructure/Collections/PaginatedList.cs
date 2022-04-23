namespace BankSystem.Web.Infrastructure.Collections
{
    using System.Collections;
    using System.Collections.Generic;
    using Interfaces;

    public class PaginatedList<T> : IPaginatedList, IEnumerable<T>
    {
        private readonly IEnumerable<T> data;

        public PaginatedList(
            IEnumerable<T> data,
            int pageIndex,
            int totalPages,
            int surroundingPageCount)
        {
            this.data = data;

            PageIndex = pageIndex;
            TotalPages = totalPages;
            SurroundingPagesCount = surroundingPageCount;
        }

        public IEnumerator<T> GetEnumerator() => data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int PageIndex { get; }

        public int TotalPages { get; }

        public bool HasPreviousPage => PageIndex > 1;

        public bool HasNextPage => PageIndex < TotalPages;

        public int SurroundingPagesCount { get; }
    }
}