namespace EBOSP.Contracts.MasterData;

public sealed record WarehouseResponse(Guid Id, Guid BranchId, string Name, string Status);
