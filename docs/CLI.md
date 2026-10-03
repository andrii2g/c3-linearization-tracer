# CLI behavior

## Grammar
```text
c3-tracer trace <input.json> [--target NAME] [--json PATH] [--svg PATH]
                [--max-events N] [--no-trace] [--force]
c3-tracer validate <input.json> [--json PATH] [--force]
c3-tracer --help
c3-tracer --version
```
Accept options before or after the one positional input following the command. No short aliases, stdin input, glob expansion, output-to-stdout path `-`, `--option=value`, duplicate options or unknown options. Missing option values are usage errors. --max-events accepts decimal integers from 0 through 100000, default 20000. --no-trace sets the event cap to zero and suppresses text events; reject its combination with --max-events. Summary output remains. validate rejects --target, --svg, --max-events and --no-trace. --force requires at least one output path. No arguments prints concise usage to stderr and exits 2. Help/version exit 0.

## Exit and stream rules
| Exit | Meaning |
|---|---|
| 0 | Successful trace or structurally valid graph |
| 1 | Filesystem/permission/write failure or unexpected internal error |
| 2 | Usage, JSON, schema, limits, references, target or inheritance cycle error |
| 3 | Acyclic graph with inconsistent C3 precedence in selected closure |

Human-readable successful trace and expected C3 failure explanation go to stdout. Input diagnostics go to stderr. JSON is written only to the requested file, never mixed with stdout text. Expected failure artifacts must be attempted before exit 3. Operational write failure takes precedence and returns 1. Do not dump stack traces by default.

## Input failure reporting
If parsing/validation fails and --json was supplied, write an invalidInput report with diagnostics. If --svg was supplied, write a simple diagnostic SVG panel with error codes and messages; do not attempt graph layout for cyclic or invalid graphs. --force rules still apply. For command-line argument errors and input I/O failure, artifacts are not required.

## Paths and writes
Resolve input and output paths relative to the current directory and normalize to full paths internally. Refuse outputs that alias input or each other (case-insensitive comparison on Windows, ordinal on Linux). Resolve existing symlinks where supported; reject existing symlink output destinations so --force cannot replace or follow them ambiguously. Document that this is a local CLI, not a hostile-filesystem sandbox.

Preflight every destination before any writes. Existing output requires --force. Create missing parent directories. Serialize both outputs to temporary files in their own destination directories; close streams before rename. Only then publish each file via rename/move, with overwrite allowed only under --force. Multi-file publication is not a transaction: if the second rename fails, return exit 1 and report which output already succeeded. Clean up unused temporary files. Never overwrite input, even with --force.

## Text rendering
Show requested target; for each class show ordered bases, merge sequences, step, candidate reason and partial order. A blocker should read `O rejected: occurs at remaining index 1 of base:1 [B, O]; B must precede O`. Show all blockers in structured output; text may wrap but must not silently omit them. On truncation print retained/dropped event counts and make clear the result was fully computed. Failure identifies failing class and marks its partial prefix explicitly as incomplete.

Final metrics use names from CONTRACTS and invariant formatting. Avoid color control characters unless later explicitly requested; plain text must redirect cleanly.
