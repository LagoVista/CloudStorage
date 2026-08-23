# Card 1 - Stabilize Mongo IDocumentCollection Adapter

## Objective

Finish and validate the initial Mongo implementation behind `IDocumentCollection` without changing the richer `DocumentDBRepoBase<TEntity>` path yet.

## Status

In progress. Implementation and automated coverage are in place; local build and live Mongo validation remain.

## Current state

`MongoDocumentCollection` exists and `DocumentCollectionFactory` selects it when Mongo is configured. Typed filtering, sorting, paging, projection, and the first semantic aggregate query have initial implementations.

The MongoDB.Driver 3.5 sorting mismatch found during the first local build has been corrected by using explicit `SortDefinition<TDocument>` instances rather than `IFindFluent.SortBy(...)`.

## Completed

- Added focused provider-selection and adapter-construction tests.
- Added a regression test confirming Cosmos remains the default provider.
- Added a serialization test confirming CLR `Id` becomes Mongo `_id`.
- Added an architecture test confirming `IDocumentCollection` exposes no Cosmos SDK types.
- Added optional live Mongo integration coverage for typed filtering, ascending sorting, paging, paged projection, unpaged projection, and `CustomerIndustryNicheSalesStageCounts`.
- Made environment-variable provider tests non-parallel and restore prior process settings.

## Remaining validation

- Build `LagoVista.CloudStorage` and the CloudStorage integration test project locally.
- Run the normal unit tests.
- Run the live Mongo tests against the dev Mongo instance.

The live Mongo fixture is skipped unless this environment variable is set:

```text
NUVIOT_TEST_MONGO_CONNECTION_STRING=mongodb://...
```

Suggested validation commands from the repository root:

```powershell
dotnet build src/LagoVista.CloudStorage/LagoVista.CloudStorage.csproj
dotnet test tests/LagoVista.CloudStorage.Tests/LagoVista.CloudStorage.IntegrationTests.csproj
```

With `NUVIOT_TEST_MONGO_CONNECTION_STRING` set, the second command also exercises the live Mongo adapter.

## Tasks

- [ ] Build `LagoVista.CloudStorage` against the current MongoDB.Driver version and clear any remaining compiler/API mismatches.
- [x] Add focused tests for provider selection and Mongo adapter construction.
- [x] Add live-test coverage for typed filter translation.
- [x] Add live-test coverage for ascending sorting for both string and generic sort expressions.
- [x] Add live-test coverage for paged typed projection.
- [x] Add live-test coverage for unpaged typed projection.
- [x] Add live-test coverage for `CustomerIndustryNicheSalesStageCounts` against Mongo.
- [x] Confirm CLR `Id` maps to Mongo `_id` as expected.
- [x] Confirm no Cosmos SDK types leak through `IDocumentCollection`.
- [ ] Execute the live Mongo fixture and confirm all adapter operations pass.

## Acceptance criteria

- CloudStorage builds cleanly.
- Existing Cosmos behavior remains unchanged when no provider setting is supplied.
- Mongo provider can execute all `IDocumentCollection` methods used by current callers.
- The semantic customer metrics query returns equivalent results from Cosmos and Mongo fixtures.
- Identity behavior is documented and tested.

## Out of scope

- Full CRUD parity for `DocumentDBRepoBase<TEntity>`.
- Migration tooling.
- Domain-based collection routing.
