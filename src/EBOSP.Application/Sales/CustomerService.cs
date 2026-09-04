using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Sales;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Sales;

public sealed class CustomerService(ICustomerRepository customers, IDomainEventRecorder events, IUnitOfWork unitOfWork) : ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(Guid tenantId, Guid actingUserId, CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(tenantId, request.Name, request.CreditLimit);
        await customers.AddAsync(customer, cancellationToken);

        events.Record("CustomerCreated", tenantId, nameof(Customer), customer.Id.ToString(), new { customer.Name, customer.CreditLimit }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid tenantId, Guid actingUserId, Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await GetOwnedAsync(tenantId, id, cancellationToken);
        customer.Update(request.Name, request.CreditLimit);

        events.Record("CustomerUpdated", tenantId, nameof(Customer), customer.Id.ToString(), new { customer.Name, customer.CreditLimit }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var customer = await GetOwnedAsync(tenantId, id, cancellationToken);
        customer.Activate();

        events.Record("CustomerActivated", tenantId, nameof(Customer), customer.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid tenantId, Guid actingUserId, Guid id, CancellationToken cancellationToken)
    {
        var customer = await GetOwnedAsync(tenantId, id, cancellationToken);
        customer.Deactivate();

        events.Record("CustomerDeactivated", tenantId, nameof(Customer), customer.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<PagedResult<CustomerResponse>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await customers.ListAsync(tenantId, request, cancellationToken);
        return new PagedResult<CustomerResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task<Customer> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await customers.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Customer not found.");

    private static CustomerResponse ToResponse(Customer customer) => new(customer.Id, customer.Name, customer.CreditLimit, customer.Status.ToString());
}
