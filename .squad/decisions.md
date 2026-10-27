# Decisions Log

(append-only, merged by Scribe from decisions/inbox/)

### 2026-09-14T00:00:00Z: API pagination convention — all collection endpoints must be paginated
**By:** Cyrille NDOUMBE (via Copilot) — cross-squad directive, see root `.squad/decisions.md`
**What:** Any API endpoint returning a list/collection of resources must return paginated content, unless the user explicitly asks for no pagination.
- Query params: `page` (1-based) and `pageSize`.
- Server enforces a default page size (50) and a configurable max page size (200) — page size is always the backend's responsibility.
- Response body shape: `{ "items": [...], "links": { "prev": {"href", "rel"}, "next": {...}, "first": {...}, "last": {...} }, "total": <int> }`.
- Response headers: `Link` (prev/next), `X-Count`, `X-Total-Count`.
- HTTP status: `200 OK` for a single page, `206 Partial Content` when the result spans multiple pages; standard 2XX/3XX/4XX/5XX families otherwise apply.
- If pagination parameter names/index start aren't already fixed, ask the user before implementing.
**Why:** User directive to standardize collection API responses across the whole monorepo.

### 2026-08-21T00:00:00Z: Replaced FastEndpoints.Swagger with FastEndpoints.OpenApi in Documents.API
**By:** Trinity
**What:**
- `Documents.API` now references `FastEndpoints.OpenApi` (8.3.0, aligned with the other FastEndpoints packages already on 8.3.0) instead of `FastEndpoints.Swagger` (8.3.0), in `Directory.Packages.props` and `Documents.API.csproj`.
- `Program.cs`: `.SwaggerDocument(...)` replaced with `.OpenApiDocument(...)` (same `ShortSchemaNames`, `ShowDeprecatedOps`, `MaxEndpointVersion = 1`, `DocumentName = "v1"`, `Title`, `Version`, `AutoTagPathSegmentIndex = 0`). NSwag-specific options with no equivalent in the new pipeline (`SchemaSettings.AllowReferencesWithProperties`, `SerializerSettings`) were dropped since JSON options are already applied globally via `Configure<JsonOptions>`.
- Added `Documents.API.TypeMappers.NumberTypeSchemaTransformer<TNumber, TValue>` (an `IOpenApiSchemaTransformer`, copied from `Agenda.API`'s pattern) and wired it via `ConfigureOpenApi`/`AddSchemaTransformer` for `PositiveInteger`, `NonNegativeInteger`, and `NonNegativeLong` (the latter needed because Documents.API — unlike Agenda.API — has a `long`-backed wrapper).
- `app.UseOpenApi(...)` replaced with `app.MapOpenApi().AllowAnonymous()`; `MapScalarApiReference` switched from `AddDocuments([new ScalarDocument(...)])` to `AddDocument("v1")` (matching Agenda.API's pattern) while keeping `ForceDarkMode()`. The `/scalar/{documentName}/{**asset}` redirect and anonymous access are unchanged.
- `NumberTypeMapper.cs` (the legacy NSwag `ITypeMapper`) was kept as-is (unused but not referenced elsewhere), same as the equivalent file in `Agenda.API`. No existing unit tests referenced `SwaggerDocument`, `FastEndpoints.Swagger`, or `NumberTypeMapper`, so no test changes were needed.
**Why:** Align Documents.API with the already-migrated Agenda.API pattern (see agenda's decision on replacing Swagger UI with Scalar while keeping FastEndpoints/OpenAPI document generation). `dotnet build` and the `Documents.API.UnitTests` suite both pass after the change; `/scalar` remains reachable and anonymous.
