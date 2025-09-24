namespace ProjectQLDCCT.Models.DTOs
{
    public class DataTableResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool Success { get; set; } = true;
    }
}
