# Coverage Board

A deliberately small coordination mechanism for finite coverage efforts.

This is not a project-management system. It exists to answer one question: **what have we covered, what are we working on, and what is left?**

## Convention

Every board lives in a `.coverage/` folder at the repository root:

- `board.json` — board metadata and the ordered list of card files.
- `cards/*.json` — one card per meaningful thing to cover.
- `viewer.html` — read-only three-lane visualizer.

There are exactly three workflow states:

- `backlog`
- `in-process`
- `completed`

Cards may use any category appropriate to the effort. For test coverage, the initial categories are `unit-test`, `integration-test`, and `parity-test`.

## Card philosophy

A card tracks coverage of a **thing**, not individual test cases or implementation steps. The test suite remains the source of truth for individual tests and whether they pass.

A test card should normally identify:

- the component or contract being covered;
- the kind of coverage expected;
- the source file or files involved;
- the test fixture or fixtures once they exist;
- the runtime environment when relevant.

Move a card to `completed` when the intended coverage exists and is represented by passing automated tests. Do not mirror each test method into the card.

## Viewing the board

From the repository root, serve the files with any simple static HTTP server, for example:

```powershell
python -m http.server 8080
```

Then open:

`http://localhost:8080/.coverage/viewer.html`

The viewer is intentionally read-only. Update the JSON cards in source control and refresh the page.
