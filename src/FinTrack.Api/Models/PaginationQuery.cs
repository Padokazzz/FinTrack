using System.ComponentModel.DataAnnotations;

namespace FinTrack.Api.Models;

public class PaginationQuery
{
    [Range(1, int.MaxValue / 100)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
