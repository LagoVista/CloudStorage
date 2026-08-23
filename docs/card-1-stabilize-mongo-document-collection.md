# Card 1 - Stabilize Mongo IDocumentCollection Adapter

## Objective

Finish and validate the initial Mongo implementation behind `IDocumentCollection` without changing the richer `DocumentDBRepoBase<TEntity>` path yet.

## Current state

`MongoDocumentCollection` exists and `DocumentCollectionFactory` selects it when Mongo is configured. Typed filtering, sorting, paging, projection, and the first semantic aggregate query have initial implementations.

## Tasks

- Build `LagoVista.CloudStorage` against the current MongoDB.Driver version and clear compiler/API mismatches.
- Add focused tests for provider selection and Mongo adapter construction.
- Validate typed filter translation.
- Validate ascending sorting for both string and generic sort expressions.
- Validate paged typed projection.
- Validate unpaged typed projection.
- Validate `CustomerIndustryNicheSalesStageCounts` against Mongo.
- Confirm CLR `Id` maps to Mongo `_id` as expected.
- Confirm no Cosmos SDK types leak through `IDocumentCollection`.

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
