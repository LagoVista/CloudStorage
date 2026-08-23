# Card 1 - Stabilize Mongo IDocumentCollection Adapter

## Objective

Finish and validate the initial Mongo implementation behind `IDocumentCollection` without changing the richer `DocumentDBRepoBase<TEntity>` path yet.

## Status

Complete. The lower-level Mongo adapter now builds and passes live integration validation against the repository-owned local Mongo 8 Docker harness.

## Current state

`MongoDocumentCollection` exists and `DocumentCollectionFactory` selects it when Mongo is configured. Typed filtering, sorting, paging, projection, and the first semantic aggregate query are implemented and live-tested.

The MongoDB.Driver 3.5 sorting mismatch found during the first local build was corrected by using explicit `SortDefinition<TDocument>` instances rather than `IFindFluent.SortBy(...)`.

Mongo BSON configuration now also preserves the migration wire contract: the root document identity maps to Mongo `_id`, while nested `EntityHeader.Id` values remain `Id`.

## Completed

- [x] Build `LagoVista.CloudStorage` against the current MongoDB.Driver version.
- [x] Add focused provider-selection and adapter-construction tests.
- [x] Add a regression test confirming Cosmos remains the default provider.
- [x] Add live-test coverage for typed filter translation.
- [x] Add live-test coverage for ascending sorting for both string and generic sort expressions.
- [x] Add live-test coverage for paged typed projection.
- [x] Add live-test coverage for unpaged typed projection.
- [x] Add live-test coverage for `CustomerIndustryNicheSalesStageCounts` against Mongo.
- [x] Confirm CLR root `Id` maps to Mongo `_id` as expected.
- [x] Confirm nested `EntityHeader.Id` remains `Id` and matches the migration document shape.
- [x] Confirm no Cosmos SDK types leak through `IDocumentCollection`.
- [x] Execute the live Mongo fixture against local Docker Mongo 8.

## Validation evidence

On August 23, 2026, the Mongo integration suite completed with:

```text
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0, duration: 1.1s
```

The repository-owned harness runs Mongo 8 on `localhost:27018` and is launched with:

```powershell
./tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1
```

## Acceptance criteria

- [x] CloudStorage builds successfully.
- [x] Existing Cosmos behavior remains unchanged when no provider setting is supplied.
- [x] Mongo provider can execute all `IDocumentCollection` methods used by current callers.
- [x] The semantic customer metrics query returns the expected server-side aggregate result from Mongo.
- [x] Identity behavior is documented and tested.

## Out of scope

- Full CRUD parity for `DocumentDBRepoBase<TEntity>`. See Cards 5 and 6.
- Migration tooling. See Card 4.
- Domain-based collection routing. See Card 3.
