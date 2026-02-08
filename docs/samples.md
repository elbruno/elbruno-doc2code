# Using the Sample Requirements Files

## Where to Find Them

The `samples/` directory at the repository root contains three ready-to-use
requirements documents that you can upload through the Web frontend to test the
full agent pipeline.

| File | Language | Domain |
|------|----------|--------|
| `sample-library-management.txt` | Spanish | Municipal library (books, readers, loans) |
| `sample-task-tracker.txt` | English | Project/task tracking (projects, tasks, members) |
| `sample-inventory-system.txt` | Spanish | Warehouse inventory (products, stock movements, purchase orders) |

## Document Structure

Each sample follows a consistent format that the Analyst agent is trained to parse:

1. **Title** — one-line heading
2. **Overview / Introduction** — brief description of the system
3. **Actors** — who interacts with the system and their responsibilities
4. **Domain Entities** — data structures with field names, types, and constraints
5. **Business Rules** — numbered rules (RN01, BR01, etc.) with when/then logic
6. **State Machines** — lifecycle transitions for key entities
7. **Non-Functional Requirements** — performance, security, audit expectations

## Writing Your Own

When creating a custom requirements document:

- Use plain text (`.txt`) — the `TextDocumentIngester` reads UTF-8 text files
- Aim for 40–100 lines; very short documents may produce sparse output
- Include at least 3 entities, 2 actors, and 4 business rules for best results
- Define at least one state machine to exercise the Analyst's lifecycle extraction
- You can write in any language — the LLM handles translation internally
- Structure headings with `#` or `##` so the ingester can segment the document

## Running a Sample

1. Start the application via `dotnet run --project src/elbruno.Doc2Code.AppHost`
2. Open the Web frontend from the Aspire dashboard
3. On the Home page, upload one of the sample files
4. Watch the pipeline progress in real time via the agent log panel
5. Once complete, download the generated solution archive
