# Pipeline Designer

The Pipeline Designer is a visual DAG (directed acyclic graph) editor for composing and customising agent pipelines. It provides a drag-and-drop canvas where users can add agents, connect them with directed edges, and configure step-level properties such as retry policies and output key mappings.

## Layout

The designer page is divided into three panels:

| Panel | Location | Contents |
|-------|----------|----------|
| **Agent Repository** | Left sidebar | Built-in agents, custom agents, and pipeline templates |
| **Canvas** | Centre | Toolbar and interactive node canvas with SVG edge overlay |
| **Properties Inspector** | Right sidebar | Selected step configuration and edge management |

## Adding Agents

Click any agent in the left sidebar to place it on the canvas. Agents land **unconnected** — no automatic edges are created. Bookend agents (Document Input and Generated Assets) are present in every pipeline template but are excluded from the sidebar list to prevent duplicates.

## Creating Connections

There are two methods to create a directed edge between steps:

### Primary — Drag from Port to Port

Each node displays two connector ports:

- **Output port** (●, right side) — green circle on the right edge of the node
- **Input port** (●, left side) — green circle on the left edge of the node

Bookend exceptions:

- **Document Input** (start node) has an output port only
- **Generated Assets** (end node) has an input port only

To connect:

1. Press and hold on the **output port** of the source node.
2. A dashed yellow line follows the cursor.
3. Drag to the **input port** of the target node — valid targets glow yellow on hover.
4. Release to create the edge.
5. Releasing over the canvas background cancels the connection.

### Secondary — Click-to-Click (Connect Mode)

1. Click the **↔ Connect** button in the toolbar (or click "Add Connection From This Step" in the properties panel).
2. Click the source node.
3. Click the target node.
4. The edge is created and Connect mode exits.

Self-loop connections and duplicate edges are rejected by both methods.

## Connection Direction

Edges flow **left-to-right** with arrowheads indicating direction. This direction determines the execution order during pipeline runs. The `OutputKeyMapping` on each edge specifies which output key the downstream step reads from the upstream step's output.

Edge labels showing the `OutputKeyMapping` value are displayed at the midpoint of each edge on the canvas.

## Selecting Nodes

Click a node body (not a port) to select it. The selected node is highlighted with a green border glow, and its properties appear in the right sidebar.

## Moving Nodes

Drag a node body to reposition it on the canvas. Edges dynamically redraw as nodes move.

## Removing Steps

1. Select a step by clicking it.
2. Click the **✖ Remove Step** button in the properties panel.

Bookend steps (Document Input and Generated Assets) are protected and cannot be removed.

## Removing Edges

In the properties panel, each inbound and outbound edge has a **×** delete button. Click it to remove the edge.

## Templates

The "Templates" section in the left sidebar provides pre-configured pipelines. Click a template to create a new pipeline from it:

| Template | Description |
|----------|-------------|
| **Blank** | Only start/end bookend nodes — build from scratch |
| **Default** | Full 6-agent pipeline with parallel Testing ∥ Documentation |
| **Code Only** | Minimal: Analyst → Developer |
| **Quick Prototype** | Analyst → Developer → Documentation |
| **Full QA** | All agents in strict sequence with strict quality gate (score ≥ 80, max 3 retries) |

## Toolbar Actions

| Button | Action |
|--------|--------|
| **💾 Save** | Persist the current pipeline to the backend |
| **Pipeline dropdown** | Switch between saved pipelines |
| **Clone** | Duplicate the current pipeline |
| **Delete** | Delete the current pipeline (disabled for the default pipeline) |
| **Set Active** | Mark this pipeline as the active one for generation runs |
| **✓ Validate** | Run DAG validation rules |
| **Layout** | Auto-arrange nodes using topological ordering |
| **↔ Connect** | Toggle click-to-click connect mode |
| **↩ / ↪** | Undo / Redo |
| **Export JSON** | Download the pipeline definition as a JSON file |
| **Import JSON** | Paste and import a pipeline definition from JSON |

## Validation

The Validate button checks the pipeline against these rules:

1. Must have at least one step.
2. No duplicate step IDs.
3. All steps must reference existing agent keys.
4. All edges must reference existing steps.
5. Must have at least one root step (no inbound edges) and one leaf step (no outbound edges).
6. No cycles — verified via Kahn's algorithm.
7. Document Input and Generated Assets bookend steps are required.
8. **Warning:** missing input key coverage from upstream edges.

## Properties Inspector

When a step is selected, the right panel shows:

- **Agent** — display name and agent key
- **Step ID** — unique identifier within the pipeline
- **Model Override** — the LLM model configured for this agent
- **Temperature** — creativity/randomness slider
- **Retry Policy** — toggle on/off; configure max retries, quality gate field, and acceptance threshold
- **Inbound Edges** — list of incoming connections with editable output key mapping and delete button
- **Outbound Edges** — list of outgoing connections with delete button
- **Add Connection** — shortcut button to enter Connect mode from this step
- **Remove Step** — delete the selected step (disabled for bookend steps)

## Keyboard Shortcuts (Planned)

| Key | Action |
|-----|--------|
| `Delete` | Remove selected node or edge |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Escape` | Cancel connect mode |
