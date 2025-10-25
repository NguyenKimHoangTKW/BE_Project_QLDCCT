using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Models.DTOs;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;

namespace ProjectQLDCCT.Helpers
{
    public static class DataTableHelper
    {
        public static async Task<object> GetDataTableAsync<T>(
            IQueryable<T> query,
            DataTableRequest request)
        {
            var totalRecords = await query.CountAsync();
            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new
            {
                data,
                totalRecords,
                request.Page,
                request.PageSize,
                totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
                success = true
            };
        }
    }
}
