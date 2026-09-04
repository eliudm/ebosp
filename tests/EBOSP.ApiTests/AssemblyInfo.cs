using System.Runtime.CompilerServices;

// EBOSP.E2ETests reuses this project's test helpers (CustomWebApplicationFactory subclasses,
// AuthTestHelpers, ProcurementTestHelpers, BillingTestHelpers, SalesTestHelpers, etc.) to build
// continuous cross-module journeys rather than duplicating them - dev guide §25.3.
[assembly: InternalsVisibleTo("EBOSP.E2ETests")]
