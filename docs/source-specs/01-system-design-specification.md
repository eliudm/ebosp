# Enterprise Business Operations & Security Platform — Technical Documentation

**Full System Design, Architecture, Data Flow, Event Flow and Implementation Specification**

Technology target: C# / ASP.NET Core + React / TypeScript + PostgreSQL or SQL Server + Azure

Document Version: 1.0 | Prepared: 30 August 2026

> Transcribed verbatim (text content) from `Enterprise_Business_Operations_Security_Platform_Full_Documentation.pdf` for in-repo reference. Diagrams referenced as figures (DFDs, ERD, event flows, deployment, CI/CD) exist as images in the source PDF and are not reproduced here; retain the original PDF in team documentation storage as the visual source of truth. See `02-development-guide.md` for the companion execution playbook.

---

## 1. Executive Summary

The Enterprise Business Operations & Security Platform (EBOSP) is a production-style, modular business application designed to demonstrate end-to-end full-stack engineering capability. It combines operational workflows—inventory, procurement, sales, invoicing and payments—with security, auditability, reporting, real-time notifications, anomaly detection, document management and an optional AI assistant.

The architecture deliberately starts as a modular monolith rather than a distributed microservice estate. This keeps the first release understandable and deployable while preserving clear domain boundaries so selected components can be extracted later if scale or organizational requirements justify it.

The design is aligned conceptually with Microsoft's Azure Well-Architected Framework pillars: Reliability, Security, Cost Optimization, Operational Excellence and Performance Efficiency.

- **Primary goal:** demonstrate enterprise-grade .NET and React engineering, not merely CRUD development.
- **Primary users:** administrators, managers, procurement officers, warehouse staff, sales staff, finance staff, auditors and business owners.
- **Core architectural style:** modular monolith + asynchronous domain events + REST API + relational database.
- **Security model:** authentication, MFA-ready identity, RBAC/policy authorization, tenant isolation, audit logging and anomaly detection.
- **Deployment target:** Azure with Docker and CI/CD; local development remains fully possible with containers.

## 2. Business Objectives

- Centralize operational business transactions in one system.
- Provide controlled workflows for purchasing, sales, approvals and payments.
- Maintain an auditable stock ledger and transaction history.
- Prevent unauthorized access to data and business actions.
- Provide management dashboards and operational reporting.
- Detect unusual activity and generate actionable security alerts.
- Provide real-time notifications for approvals, low stock, payments and security events.
- Support multiple companies/tenants and branches without mixing tenant data.
- Provide APIs and documented integration points for future external systems.
- Demonstrate modern software delivery through automated tests, CI/CD, containers and cloud deployment.

## 3. Scope

| Module | Release 1 scope | Later enhancement |
|---|---|---|
| Identity & Access | Login, roles, permissions, MFA-ready, sessions/tokens | SSO, passkeys, conditional access |
| Tenant & Branches | Company, branches, warehouses, data isolation | Advanced tenant plans and billing |
| Inventory | Products, stock ledger, receiving, issuing, transfers, adjustments | Serial/lot tracking, forecasting |
| Procurement | Requests, approvals, POs, receipts, supplier invoices | Supplier portal, RFQ automation |
| Sales | Customers, quotations, orders, delivery, invoicing, payments | Customer portal, subscriptions |
| Finance | Invoices, payments, balances, basic financial summaries | Full accounting/GL integration |
| Workflow | Configurable approval stages and thresholds | Visual workflow designer |
| Security | Audit trail, failed-login detection, anomaly rules | SIEM/SOAR integration |
| Notifications | In-app, email abstraction, real-time SignalR | SMS/WhatsApp provider adapters |
| Reporting | Operational dashboards and exports | Data warehouse and advanced BI |
| Documents | Secure attachment metadata and storage | OCR/classification |
| AI Assistant | Read-only business queries | Forecasting and agentic workflows |

## 4. Actors and Roles

| Role | Primary responsibilities |
|---|---|
| System Administrator | Tenant setup, users, roles, security settings, system configuration |
| Company Administrator | Company-level users, branches, products, workflows |
| Manager | Approvals, dashboards, exceptions and operational oversight |
| Procurement Officer | Purchase requests, supplier management, purchase orders |
| Warehouse Officer | Receipts, issues, transfers, stock counts and adjustments |
| Sales Officer | Customers, quotations, orders, deliveries and returns |
| Finance Officer | Invoices, payments, balances and financial reports |
| Auditor | Read-only access to transactions and audit trails |
| Security Officer | Security alerts, investigations and incident records |
| Employee | Create permitted requests and view authorized information |

## 5. High-Level Architecture

The platform is organized into presentation, API, application/domain, infrastructure and asynchronous processing boundaries. The business domain remains independent of UI concerns and infrastructure details.

- **Frontend:** React + TypeScript, component-based UI, client-side routing, query/cache layer and role-aware navigation.
- **API:** ASP.NET Core Web API, REST endpoints, validation, authentication, authorization, rate limiting and OpenAPI.
- **Application layer:** use cases, commands/queries, workflow orchestration and transaction boundaries.
- **Domain layer:** entities, value objects, domain rules and domain events.
- **Infrastructure:** Entity Framework Core, database, cache, object storage, identity provider adapters and external services.
- **Async layer:** event bus/service bus abstraction and background workers for notifications, analytics and non-critical processing.
- **Observability:** structured logs, metrics, traces, audit events and health checks.

*(Figure 2 — Level 1 DFD showing the principal application and data flows: Web/Mobile UI → API Gateway ↔ Identity & Access [token validation] → Business Services [authorized commands] → Operational DB [transactions] and Object Storage [documents]; Business Services → Event Bus [domain events] → Analytics, Notifications, Audit & Security [events]; Event Bus aggregates → Reporting DB.)*

## 6. Context Data Flow

External actors exchange business and identity information with the platform. The platform remains the system of record for operational transactions while external providers are treated as integrations.

*(Figure 1 — System context: Users ↔ Platform [requests/data]; Suppliers ↔ Platform [quotes/invoices]; Platform → Analytics [events/metrics]; Payment Provider ↔ Platform [payment status]; Identity Provider ↔ Platform [identity claims].)*

## 7. Core Domain Model

The conceptual model below identifies the major relationships. Exact physical schema names may evolve during implementation.

*(Figure 10 — Conceptual ERD: Tenant 1:N User/Branch; Branch 1:N Warehouse; Warehouse 1:N StockBalance; Product 1:N StockBalance; Supplier 1:N PurchaseOrder 1:N PurchaseOrderLine; Customer 1:N SalesOrder 1:N SalesOrderLine 1:N Invoice 1:N Payment; AuditEvent N:1 Product/User.)*

| Entity | Purpose | Key fields |
|---|---|---|
| Tenant | Company/customer boundary | TenantId, Name, Status, TimeZone |
| User | Identity and business actor | UserId, TenantId, Email, Status |
| Role / Permission | Authorization | RoleId, PermissionId, Scope |
| Branch | Operational location | BranchId, TenantId, Name |
| Warehouse | Inventory location | WarehouseId, BranchId |
| Product | Item master | ProductId, SKU, Name, TaxCode, ReorderLevel |
| StockLedgerEntry | Immutable stock movement | EntryId, ProductId, WarehouseId, Quantity, EventType |
| StockBalance | Current stock projection | ProductId, WarehouseId, QuantityOnHand, Reserved |
| Supplier | Procurement counterparty | SupplierId, Name, TaxId, Contact |
| PurchaseRequest | Internal purchasing request | RequestId, RequestedBy, Amount, Status |
| PurchaseOrder | Supplier order | POId, SupplierId, Status, Total |
| GoodsReceipt | Proof of delivery/receipt | ReceiptId, POId, ReceivedBy |
| Customer | Sales counterparty | CustomerId, Name, CreditLimit |
| SalesOrder | Customer order | OrderId, CustomerId, Status, Total |
| Invoice | Billable document | InvoiceId, CustomerId/SupplierId, Total, Status |
| Payment | Settlement transaction | PaymentId, InvoiceId, Amount, Method |
| WorkflowInstance | Approval process state | WorkflowId, EntityType, EntityId, Status |
| AuditEvent | Security/business trace | AuditId, ActorId, Action, Resource, Timestamp |
| SecurityAlert | Risk/anomaly record | AlertId, Severity, Rule, Status |
| Document | Attached file metadata | DocumentId, EntityType, EntityId, StorageKey |

## 8. Functional Requirements

### 8.1 Identity and Access
- Users can sign in and sign out securely.
- Access tokens/session state must be short-lived and renewable through a secure mechanism.
- Roles and permissions are stored centrally and evaluated server-side.
- Sensitive actions require explicit authorization policies.
- Failed authentication attempts are rate-limited and logged.
- MFA should be supported as an implementation milestone.
- All security-sensitive changes generate audit events.

### 8.2 Inventory
- Create and maintain products and categories.
- Receive stock against a purchase order or approved receipt.
- Issue stock against sales fulfillment or authorized internal issue.
- Transfer stock between warehouses.
- Perform controlled stock adjustments with reason codes.
- Maintain an append-only stock ledger.
- Calculate current balances from ledger/projection.
- Trigger low-stock notifications.
- Prevent unauthorized negative stock operations according to configurable policy.

### 8.3 Procurement
- Employees create purchase requests.
- Approval rules route requests based on amount, department or category.
- Approved requests can generate purchase orders.
- Goods receipt updates inventory.
- Supplier invoices are linked to orders/receipts.
- Payments are recorded only by authorized finance users.

### 8.4 Sales
- Create quotations and convert accepted quotations to orders.
- Validate customer status and credit policy.
- Reserve or allocate stock.
- Create delivery/fulfillment records.
- Generate invoices after defined fulfillment conditions.
- Record payments and outstanding balances.
- Support returns and credit notes in later releases.

### 8.5 Audit and Security
- Record authentication, authorization and high-value business events.
- Protect audit records from ordinary users.
- Detect abnormal transaction sizes, repeated failures and suspicious access patterns.
- Provide security investigation screens.
- Allow security officers to acknowledge, investigate and close alerts.

### 8.6 Reporting
- Sales and revenue summaries.
- Inventory valuation and movement.
- Low-stock and slow-moving inventory.
- Procurement spend.
- Outstanding invoices and payments.
- Approval turnaround time.
- Security events and alerts.

## 9. Data Flow Diagrams

*(Section contains Context DFD and Level 1 DFD figures — see Figures 1 and 2 above; source PDF holds the rendered diagrams.)*

## 10. Event-Driven Design

The system uses domain events to decouple secondary actions from the primary transaction. A business transaction should commit its critical data atomically; non-critical work such as notifications, analytics and anomaly processing can be handled asynchronously.

**Recommended event envelope:**

| Field | Description |
|---|---|
| eventId | Globally unique event identifier |
| eventType | Stable event name such as StockReceived |
| occurredAt | UTC timestamp |
| tenantId | Tenant boundary |
| correlationId | End-to-end request/workflow correlation |
| causationId | Event or command that caused this event |
| actorId | Authenticated actor where applicable |
| aggregateType | Business aggregate type |
| aggregateId | Aggregate identifier |
| version | Event schema version |
| payload | Business-specific event data |

### 10.1 Procurement Event Flow

*(Figure 3 — Employee → submit → Purchase Request → ApprovalEngine [approval event] → Manager [approve/reject] → approved → Purchase Order → PO → Supplier → delivery → Goods Receipt → stock received → Inventory; Goods Receipt → invoice reference → Invoice → verification → Finance → payment → Payment → PaymentCreated → Audit Log.)*

A purchase request is submitted, routed through an approval workflow, converted to a purchase order, fulfilled by a supplier, received into inventory, matched to an invoice and finally settled. Each significant state transition emits an auditable event.

### 10.2 Sales Event Flow

*(Figure 4 — Sales User → create → Quotation → accept → Sales Order → validate → Credit Check → reserve stock → Inventory → pick/pack → Fulfillment → dispatch → Delivery → delivered → Invoice → settle → Payment → post → Accounting; Payment → PaymentReceived → Audit.)*

The sales lifecycle validates the customer, checks or reserves inventory, fulfills the order, invoices the customer and records payment.

### 10.3 Inventory Event Flow

*(Figure 5 — Purchase Receipt [StockReceived], Sales Shipment [StockIssued], Transfer [StockTransferred], Adjustment [StockAdjusted] → Stock Ledger → immutable event → Audit Log; Stock Ledger → recalculate → Stock Balance → compare → Reorder Rules → LowStockAlert → Notification Service.)*

The stock ledger is the authoritative history of stock movements. A balance projection is maintained for fast reads, while the ledger supports auditability and reconciliation.

### 10.4 Authentication Event Flow

*(Figure 6 — User [credentials] → Login UI → authenticate → Identity Service → challenge → MFA → issue → Token [Bearer token] → API; Business Service → allow/deny ← Policy Engine [roles/claims] ← API; Business Service → access event → Audit.)*

Authentication proves identity; authorization determines whether the identity can perform a particular operation. Authorization must be enforced at the API/server rather than relying only on frontend visibility.

### 10.5 Security Event Flow

*(Figure 7 — Application [security event] → Events Collector → normalize → Rules Engine → evaluate → Risk Scorer → threshold → Security Alert → notify → SOC/Admin → lock/revoke/investigate → Response Actions → response record → Audit Store; Application → telemetry → SIEM/Monitoring.)*

Security events are normalized, evaluated against rules and assigned a risk/severity. Alerts can lead to account lock, session/token revocation, investigation or escalation.

## 11. Event Catalogue

| Event | Producer | Consumers | Criticality |
|---|---|---|---|
| UserCreated | Identity | Audit, Notifications | Medium |
| LoginFailed | Identity | Security, Audit | High |
| PurchaseRequestSubmitted | Procurement | Workflow, Audit | High |
| PurchaseApproved | Workflow | Procurement, Audit, Notification | High |
| PurchaseOrderCreated | Procurement | Supplier adapter, Audit | Medium |
| GoodsReceived | Warehouse | Inventory, Finance, Audit | High |
| StockReceived | Inventory | Alerts, Analytics, Audit | High |
| StockIssued | Inventory | Alerts, Analytics, Audit | High |
| StockAdjusted | Inventory | Security, Audit, Analytics | Critical |
| SalesOrderCreated | Sales | Inventory, Audit | Medium |
| InvoiceCreated | Billing | Finance, Notification, Audit | High |
| PaymentReceived | Finance | Invoice, Reporting, Audit | Critical |
| LowStockDetected | Inventory | Notification | Medium |
| SecurityAlertRaised | Security | Notification, Security dashboard | Critical |
| DocumentUploaded | Documents | Audit, Security scan | Medium |

## 12. API Specification

Recommended REST resource structure:

| Area | Example endpoints | Purpose |
|---|---|---|
| Auth | POST /api/v1/auth/login | Authentication |
| Users | GET/POST /api/v1/users | User administration |
| Products | GET/POST /api/v1/products | Product master |
| Inventory | GET /api/v1/inventory/balances | Stock balances |
| Inventory | POST /api/v1/inventory/receipts | Receive stock |
| Inventory | POST /api/v1/inventory/transfers | Transfer stock |
| Procurement | POST /api/v1/purchase-requests | Create request |
| Procurement | POST /api/v1/purchase-orders | Create PO |
| Sales | POST /api/v1/sales-orders | Create order |
| Billing | POST /api/v1/invoices | Create invoice |
| Payments | POST /api/v1/payments | Record payment |
| Workflow | GET /api/v1/workflows/{id} | Workflow status |
| Audit | GET /api/v1/audit-events | Audit search |
| Security | GET /api/v1/security/alerts | Security alerts |
| Reports | GET /api/v1/reports/sales | Sales report |

All endpoints should use consistent error envelopes, correlation IDs, pagination, filtering and authorization policies. OpenAPI should be generated from the API contract and used as the source for developer documentation.

## 13. Database Design Rules

- Use surrogate primary keys such as UUIDs where appropriate for distributed-safe identifiers.
- Every tenant-owned table must carry TenantId directly or through an unambiguous ownership relationship.
- Use UTC for persisted timestamps and store business time-zone settings per tenant/branch.
- Use database constraints for uniqueness, foreign keys and valid state relationships.
- Use decimal/numeric types for monetary values; never floating-point for money.
- Use optimistic concurrency for records likely to be edited concurrently.
- Use soft deletion only where business retention requires it; do not use soft deletion as a substitute for proper audit history.
- Treat stock ledger entries and audit events as append-only records.
- Index common filters such as TenantId, Status, CreatedAt, ProductId and WarehouseId.
- Keep reporting queries from blocking high-volume transactional writes; introduce read models or a reporting store when needed.

## 14. Security Architecture

- Use HTTPS/TLS for all network traffic.
- Hash passwords using the identity framework's approved password hashing implementation.
- Use short-lived access tokens and secure refresh/session handling.
- Enforce authorization on the server using roles and policies.
- Use least privilege for service accounts and database users.
- Validate all inputs server-side and apply output encoding where applicable.
- Use parameterized database access through EF Core.
- Apply rate limiting to authentication and sensitive endpoints.
- Protect file uploads with allowlists, size limits, content validation and isolated storage.
- Do not place secrets in source control; use environment/secret management.
- Log security-relevant actions without logging passwords, tokens or unnecessary sensitive data.
- Use correlation IDs to trace suspicious requests across API, workers and logs.
- Use dependency and container vulnerability scanning in CI.
- Base the security verification checklist on OWASP ASVS and test security controls continuously.

Microsoft's Well-Architected guidance treats security as a cross-cutting concern and highlights monitoring security events, threat modeling, security validation and incident response as part of mature workload design.

## 15. Authorization Model

Use role-based access control with policy-based authorization for high-risk actions.

| Permission | Example role | Additional rule |
|---|---|---|
| inventory.read | Warehouse/Manager | Tenant + branch scope |
| inventory.adjust | Warehouse Manager | Reason required; high-value adjustment triggers alert |
| procurement.create | Employee/Procurement | Department scope |
| procurement.approve | Manager | Cannot approve own request |
| payment.create | Finance | Finance role + transaction limit |
| security.alert.manage | Security Officer | Security scope |
| audit.read | Auditor/Admin | Read-only |
| user.manage | Admin | Tenant admin scope |

## 16. Anomaly Detection Rules

| Rule | Example trigger | Response |
|---|---|---|
| Repeated login failures | 10 failures within configured window | Rate limit + alert |
| Impossible travel | Conflicting geographic signals in short interval | Alert + session review |
| Large stock adjustment | Adjustment exceeds configurable threshold | Manager approval + alert |
| Unusual payment | Payment exceeds historical/role threshold | Finance review |
| After-hours privileged action | Admin action outside configured hours | Security alert |
| Excessive exports | Large number of records exported | Alert + audit event |
| Cross-tenant access attempt | Tenant mismatch detected | Block + critical alert |

## 17. Notification Architecture

*(Figure 8 — Domain Event → publish → Event Bus → consume → Notification Worker → check channel → Preference Service; Notification Worker → deliver → Email/SMS/Push and → SignalR → In-App Hub → status → Delivery Log; In-App Hub → read/delivery → Delivery Log.)*

Notifications should be produced from domain events rather than embedded throughout business services. A preference service determines which channels are enabled. SignalR provides real-time in-app updates while email/SMS providers are accessed through adapters.

## 18. Document Management

- Store metadata in the relational database and file bytes in object storage.
- Generate non-guessable storage keys.
- Authorize every download using the parent business entity's permissions.
- Scan or validate uploaded files before making them available.
- Restrict extensions and MIME types according to business need.
- Record uploader, upload time, entity association and access events.
- Support retention and deletion policies.

## 19. Reporting and Analytics

- Transactional database remains optimized for operational writes.
- Simple reports may query read-optimized projections.
- Heavy reporting should move to a reporting database or analytical store.
- Use pre-aggregated metrics for dashboard cards.
- Cache expensive, low-volatility reports.
- All report queries must enforce tenant scope.

## 20. Real-Time Communication

- Use SignalR for in-app notifications and status updates.
- Do not send business-critical commands over SignalR; use authenticated REST/API commands.
- Use correlation IDs to link UI notifications to originating transactions.
- Reconnect clients gracefully and reload missed state after reconnect.

## 21. Deployment Architecture

*(Figure 9 — Browser → HTTPS → CDN/WAF → static assets → React App; CDN/WAF → API traffic → ASP.NET Core API → EF Core → PostgreSQL/SQL; API → cache → Redis; API → files → Object Storage; API → events → Service Bus/Event Grid → async processing → Workers; API → logs/metrics → Azure Monitor ← telemetry ← Workers.)*

A practical first production deployment can use a managed relational database, managed application hosting, object storage, cache, a managed messaging service and centralized monitoring. The architecture should remain container-compatible so the same application can run locally, in CI and in Azure.

## 22. CI/CD Pipeline

*(Figure 11 — Developer → push/PR → Git Repository → trigger → CI Pipeline → compile → Build → unit/integration → Tests → scan → Security Scan → pass → Container Build → artifact → Staging → release → Approval → promote → Production → observe → Monitoring.)*

- Pull request triggers build, lint/static checks and tests.
- Dependency and container security checks run before release.
- A versioned container artifact is created only after validation.
- Deploy automatically to a staging environment.
- Run smoke/integration checks against staging.
- Promote to production with an approval gate for protected environments.
- Monitor deployment health and support rollback.

## 23. Testing Strategy

| Test level | What to test | Examples |
|---|---|---|
| Unit | Domain rules and services | Stock calculation, approval rules |
| Integration | Database and service boundaries | EF Core transaction, API + DB |
| API | HTTP contract and authorization | 401/403, validation, pagination |
| End-to-end | Critical user journeys | Request → approval → PO → receipt |
| Security | Access-control and input protections | Tenant isolation, IDOR attempts |
| Performance | Throughput/latency | Dashboard and inventory endpoints |
| Resilience | Failure handling | Messaging retry, DB transient error |
| Regression | Previously fixed defects | Automated regression suite |

## 24. Reliability and Resilience

- Define health checks for database, cache, messaging and external providers.
- Use retries only for transient failures and avoid retry storms.
- Use idempotency keys for payment and other retry-sensitive commands.
- Use an outbox pattern so committed business transactions are not lost before their events are published.
- Use dead-letter handling for events that repeatedly fail.
- Back up the database and periodically test restore procedures.
- Design graceful degradation for non-critical features such as notifications and analytics.

Microsoft's current Well-Architected guidance emphasizes reliability, recovery, monitoring and rehearsed failure/recovery practices, while also highlighting the trade-offs between reliability, security, cost and operational complexity.

## 25. Observability

- Structured application logs with severity and correlation IDs.
- Metrics for request rate, latency, error rate, queue depth and database health.
- Distributed traces across API and background workers.
- Business metrics such as sales value, stock adjustments and approval duration.
- Security metrics such as failed logins and alert volume.
- Dashboards and alert thresholds for production operations.

## 26. AI Assistant — Optional Differentiator

The AI assistant should initially be read-only. It should translate authorized business questions into safe queries or predefined analytical tools, then summarize results. It must never bypass authorization or directly execute unrestricted SQL.

- Question: "Which products are below reorder level?" → authorized inventory query.
- Question: "What were the top five products last month?" → sales analytics query.
- Question: "Show unusual stock adjustments." → security/anomaly query.
- Question: "Which invoices are overdue?" → receivables query.
- Future: forecasting, demand prediction and controlled workflow assistance.

## 27. Non-Functional Requirements

| Category | Target |
|---|---|
| Security | Server-side authorization, auditability, secure secrets, TLS |
| Availability | Target defined per deployment tier; health checks and recovery plan required |
| Performance | Common API requests should be low-latency under expected load |
| Scalability | Horizontal API/worker scaling without redesigning the domain |
| Maintainability | Modular boundaries, automated tests and API documentation |
| Observability | Logs, metrics, traces and security events |
| Recoverability | Backups plus tested restore procedure |
| Data integrity | Transactional writes, constraints, idempotency and audit trail |
| Accessibility | Keyboard navigation, semantic UI and accessible forms |
| Deployment | Repeatable containerized deployment through CI/CD |

## 28. Recommended Project Structure

**Backend:**
```
src/
  EBOSP.Api/
  EBOSP.Application/
  EBOSP.Domain/
  EBOSP.Infrastructure/
  EBOSP.Contracts/
  EBOSP.Worker/
tests/
  EBOSP.UnitTests/
  EBOSP.IntegrationTests/
  EBOSP.ApiTests/
```

**Frontend:**
```
frontend/
  src/
    app/
    auth/
    components/
    features/
      inventory/
      procurement/
      sales/
      finance/
      security/
      reports/
    services/
    hooks/
    types/
    routes/
```

## 29. Implementation Roadmap

| Phase | Deliverables | Outcome |
|---|---|---|
| 1. Foundation | Repo, architecture, DB, auth, CI | Running secure skeleton |
| 2. Inventory | Products, warehouses, ledger, stock rules | Operational inventory |
| 3. Procurement | Requests, approvals, POs, receipts | Procure-to-receive workflow |
| 4. Sales & Billing | Orders, delivery, invoices, payments | Order-to-cash workflow |
| 5. Security | Audit, alerts, anomaly rules | Security-focused enterprise layer |
| 6. Reporting | Dashboards, exports, read models | Management visibility |
| 7. Documents & Notifications | Object storage, SignalR, notification adapters | Operational usability |
| 8. Testing & Hardening | Integration, E2E, security, performance | Production readiness |
| 9. Azure & DevOps | Containers, deployment, monitoring | Cloud deployment |
| 10. AI | Read-only assistant and analytics | Portfolio differentiator |

## 30. Acceptance Criteria for the Flagship Portfolio Release

- A new tenant can be created and isolated from other tenants.
- An administrator can create users, assign roles and restrict permissions.
- An employee can submit a purchase request.
- The request routes through configurable approval rules.
- An approved request becomes a purchase order.
- A warehouse officer can receive goods and the stock ledger changes correctly.
- A sales user can create an order and stock is reserved/issued according to policy.
- An invoice can be created and a payment recorded.
- High-value or suspicious transactions generate security/audit events.
- Managers receive real-time notifications.
- Auditors can trace a transaction from actor to event history.
- All critical APIs have automated tests.
- The application can be built and deployed automatically from Git.
- The production deployment exposes health, logging and monitoring information.
- The README includes architecture diagrams, setup instructions, API documentation, security model and screenshots.

## 31. Portfolio and Career Positioning

The project should be presented as a flagship enterprise engineering project rather than as an "inventory app". The strongest portfolio story is that one application demonstrates backend engineering, frontend engineering, relational data modeling, authorization, security, asynchronous processing, observability, testing, cloud deployment and AI integration.

**Suggested CV entry:**

> Enterprise Business Operations & Security Platform — Designed and developed a full-stack enterprise platform using C#, ASP.NET Core, React, TypeScript and PostgreSQL/SQL Server, implementing inventory, procurement, sales, invoicing, payment workflows, configurable approvals, RBAC, audit logging, security anomaly detection, real-time notifications, reporting, automated testing, Docker-based CI/CD and Azure deployment.

## 32. Technical Decisions and Rationale

| Decision | Rationale |
|---|---|
| Modular monolith first | Lower operational complexity while preserving domain boundaries. |
| React + TypeScript | Strong component model and type safety for a large frontend. |
| ASP.NET Core | Mature .NET web/API platform and strong enterprise ecosystem. |
| Relational DB | Strong consistency, transactions and reporting for business data. |
| Domain events | Decouple notifications, analytics and security processing. |
| Outbox pattern | Prevent loss of events after successful DB transactions. |
| SignalR | Efficient real-time in-app updates. |
| Docker | Reproducible development and deployment. |
| Azure target | Strong alignment with .NET ecosystem and managed cloud services. |
| OWASP ASVS baseline | Structured application security verification and testing. |

## 33. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Scope explosion | High | Deliver phased MVP; freeze phase scope. |
| Overengineering | High | Start modular monolith; add distributed components only when justified. |
| Security flaws | Critical | Threat model, ASVS checklist, security tests and code scanning. |
| Data inconsistency | Critical | Transactions, constraints, idempotency and outbox. |
| Event duplication | Medium | Idempotent consumers and event IDs. |
| Poor performance | High | Indexes, caching, pagination, profiling and load tests. |
| Cloud cost | Medium | Right-size services and monitor spend. |
| AI hallucination | High | Read-only tools, authorization checks and deterministic data retrieval. |

## 34. Reference Architecture Sources

- Microsoft Azure Architecture Center — Application Architecture Fundamentals: https://learn.microsoft.com/en-us/azure/architecture/guide/
- Microsoft Azure Well-Architected Framework: https://learn.microsoft.com/en-us/azure/well-architected/
- Microsoft Azure Well-Architected Framework — Mission-Critical Workloads: https://learn.microsoft.com/en-us/azure/well-architected/mission-critical/
- Microsoft Azure Well-Architected Framework — Security Tradeoffs: https://learn.microsoft.com/en-us/azure/well-architected/security/tradeoffs
- OWASP Application Security Verification Standard: https://owasp.org/www-project-application-security-verification-standard/
- React Documentation: https://react.dev/
- ASP.NET Core Documentation: https://learn.microsoft.com/en-us/aspnet/core/

## 35. Conclusion

EBOSP is intentionally designed as a serious, demonstrable enterprise system. Its value comes from the combination of business workflows, security controls, event-driven processing, data integrity, testing, deployment and observability. The system can start as a focused MVP and grow into a substantial portfolio project without changing its fundamental architecture.
