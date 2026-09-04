using System.Net.Http.Json;
using EBOSP.Contracts.Sales;

namespace EBOSP.ApiTests.Sales;

internal static class SalesTestHelpers
{
    public static async Task<Guid> CreateCustomerAsync(HttpClient client, decimal creditLimit = 1_000_000m, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest { Name = name ?? "Customer " + Guid.NewGuid(), CreditLimit = creditLimit });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>())!.Id;
    }
}
