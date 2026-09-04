using EBOSP.Contracts.Assistant;

namespace EBOSP.Application.Assistant;

public interface IAssistantService
{
    Task<AskAssistantResponse> AskAsync(Guid tenantId, Guid actingUserId, AskAssistantRequest request, CancellationToken cancellationToken);
}
