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
            DataTableRequest request,
            params Expression<Func<T, string>>[] searchableFields)
        {
            // 🔹 Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                var predicates = searchableFields
                    .Select(f =>
                    {
                        var member = (MemberExpression)f.Body;
                        var propertyName = member.Member.Name;
                        return $"{propertyName}.ToLower().Contains(@0)";
                    })
                    .ToList();

                if (predicates.Any())
                {
                    var combined = string.Join(" OR ", predicates);
                    query = query.Where(combined, search);
                }
            }

            // 🔹 Filter động
            if (request.Filters != null)
            {
                foreach (var filter in request.Filters)
                {
                    var propertyName = filter.Key;
                    var value = filter.Value;

                    if (!string.IsNullOrEmpty(value))
                    {
                        query = query.Where($"{propertyName} == @0", value);
                    }
                }
            }

            // 🔹 Sort động
            if (!string.IsNullOrEmpty(request.SortColumn))
            {
                var sortExp = $"{request.SortColumn} {request.SortDirection}";
                query = query.OrderBy(sortExp);
            }
            else
            {
                query = query.OrderBy("1");
            }

            // 🔹 Pagination
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
