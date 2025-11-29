namespace Accounting.Application.Branches.Queries.Dto;

public sealed record BranchDto(
    int Id,
    string Code,
    string Name
);
