namespace ProjectQLDCCT.Models.DTOs
{
    public class DataTableRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; } = "asc";

        public Dictionary<string, string>? Filters { get; set; }
    }

}
