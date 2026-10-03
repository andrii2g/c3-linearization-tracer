# SVG rendering specification

## Inputs and ownership
CLI renderer receives validated graph, target closure and final result. Rendering must not re-run C3. Use System.Xml.Linq with the SVG namespace; no raw concatenation of unescaped user input. All text goes through XElement/XAttribute. Escape even though identifiers have a restricted grammar because messages and future labels can contain punctuation.

## Layout
Root rank is 0. rank(C)=1+max(rank(base)); use base-first order. Display roots at top, target lowest. Group nodes by rank and use ordinal node order within ranks. This simple deterministic layout need not minimize crossings.

Use a monospace font with fallback, 14px labels, 10px padding, node height 44, approximate width max(96, 18+9*nameLength), horizontal gap 36 and vertical gap 92. Each rank is horizontally centered in the available graph width. Set overall width to max(640, widest rank width + 64). Use a scalable viewBox and matching numeric width/height. Very wide graphs are allowed to scroll rather than dropping nodes or overlapping labels. Each node has a title containing full name, declared bases and target MRO index if known.

Draw edges before nodes. Edges start at the derived node's upper edge and terminate at the base node's lower edge; arrowheads therefore point upward toward bases. Use deterministic cubic paths or polylines. Long edges may cross intermediate ranks; do not pretend crossings are nodes. Label each edge with its 1-based position in the derived class's base list near its source. Node IDs are generated stable ordinal indices (n0, n1...), not raw class names.

## Success panel
Header states target and result. Every closure node appears once. Highlight target, use a restrained second color for other nodes and badge nodes with 1-based MRO positions. Below graph, add an MRO strip of boxed names, wrapping to new rows when needed. The strip must display each name in exact result order.

## Inconsistent panel
Render the acyclic target closure graph normally. Highlight failing class independently from requested target. Do not label a partial prefix as a complete order. Below graph show `No C3 linearization`, failing class, its partial prefix, and remaining sequences. Add a separate precedence-witness panel listing edges with sequence provenance and a closed cycle. Use red dashed precedence arrows if drawing that cycle, and include a legend explicitly differentiating them from inheritance arrows. A readable wrapped cycle text with a provenance table is sufficient for MVP and avoids a second graph layout.

## Invalid-input SVG
A standalone error card with title, diagnostic codes and wrapped messages. Never lay out a cyclic graph with rank recursion. Include no raw input document.

## Safety, determinism and QA
No scripts, external images, remote fonts, foreignObject or external links. Add root role=img, title and desc, and ensure color is not the only indication of failure. Use fixed theme colors and font sizes. LF serialization and stable attribute/element order; no timestamps or random marker IDs. Marker ID may be constant within each standalone SVG. Use invariant decimal formatting.

Tests: XDocument parses generated output; node count equals closure size; all direct edges retain correct base-order labels; success strip text matches MRO; special renderer text `A<&"` is escaped; no script or remote href; output deterministic. Visually inspect at least diamond and inconsistent-order after implementation. Testing XML alone is not a visual layout check.
