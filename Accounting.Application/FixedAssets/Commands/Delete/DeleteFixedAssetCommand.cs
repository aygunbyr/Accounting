using MediatR;

namespace Accounting.Application.FixedAssets.Commands.Delete;

public sealed record DeleteFixedAssetCommand(
    int Id,
    string RowVersionBase64
) : IRequest;
