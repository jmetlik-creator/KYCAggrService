# Reflection

This solution follows the guidance in the repository: a .NET 10 backend API, an SQLite-backed EF Core persistence layer, and a small service test that exercises the cache-first workflow.

## Design choices
- The API service checks the local SQLite store before calling the external Customer Data API. This keeps the first request fresh while making subsequent requests fast and resilient to restarts.
- The external client is isolated behind an interface so the service logic remains testable and reusable.
- Error handling is centralized in the API layer. User-facing validation and not-found responses are returned as HTTP results, while unexpected failures are logged and surfaced as a generic 500 response.

## Trade-offs
- SQLite was chosen for simplicity and persistence without extra infrastructure. A more production-oriented setup could use PostgreSQL or Redis depending on scale and operational needs.
- The API currently chooses a recent KYC form date and falls back to a few known dates when the first choice is not available. This keeps the integration robust without introducing additional infrastructure.

## Potential improvements
- Add a richer integration test suite around the customer client and repository layer.
- Add a background refresh or invalidation policy when the cached KYC data becomes stale.
- Move sensitive configuration such as the remote API base URL into appsettings and environment variables.
