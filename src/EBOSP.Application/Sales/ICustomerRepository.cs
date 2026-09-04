using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Customer>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
