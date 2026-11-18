# Metalama MCP server: a query editor with IntelliSense

This document plans to replace the plain query box of the served html page (section 9.3 of `mcp-server.md`) with the
Monaco editor, with completion, quick info, signature help and diagnostics computed by Roslyn in the worker. It is
implemented; section 7 records where the implementation differs from this plan.

## 1. Goals and non-goals

### 1.1. Goals

- Editing a query in the page feels like editing C# in VS Code: completion of members of the code model (`workspace.`,
  `t.Methods.`), quick info on hover, signature help in argument lists, and errors underlined as you type.
- IntelliSense sees exactly what the query compiler sees: the same references, the same imported namespaces, and the same
  globals (`workspace`, `workspaces`, `D(…)`, `Open(…)`, `ct`).
- No IntelliSense request runs user code, so the endpoints keep the property that only Run executes a query.
- The page keeps working without the editor: if the assets fail to load, the plain box of today stays.

### 1.2. Non-goals

- No IntelliSense for the agent. The agent has its own tools and writes queries without it.
- No editor in the MCP App view. Its frame cannot reach the result server in Claude (`anthropics/claude-ai-mcp#25`), and
  it does not show the query box today.
- No editor in a saved snapshot. A snapshot has no server to run a query.
- No code fixes, refactorings, rename, find references or formatting. They can come later on the same plumbing.

## 2. What already exists

- The served page shows the code of its query in an editable box, and Run posts it to `POST <token>/query`, which checks
  the token, `Origin`, `Sec-Fetch-Site` and the JSON content type before the worker runs it (`ResultServer.ServeQueryAsync`).
- `QueryCompiler.GenerateSource` wraps the code in a class deriving from `QueryBase`, after the `using` directives of
  `QueryCompiler.Namespaces`, between `#line 1 "query.cs"` and `#line hidden`. An expression is wrapped in
  `return (object)( … );`, statements are inserted as they are, and `CompileCore` tries the expression form first.
- `QueryCompiler.GetReferences` references every assembly loaded in the worker: the .NET runtime, Roslyn and Metalama.
- The worker already ships `Microsoft.CodeAnalysis.Features` and `Microsoft.CodeAnalysis.CSharp.Features` 5.12.0,
  from the same build as the `Microsoft.CodeAnalysis.CSharp` that compiles queries, because
  `Microsoft.CodeAnalysis.Workspaces.MSBuild` brings them. They are not the Roslyn fork of `Metalama.Compiler`: the
  worker compiles queries with stock Roslyn, and so will IntelliSense. No assembly is added to the package for the back
  end, and there is no version to keep in step beyond `RoslynMaxVersion`.

## 3. Design

### 3.1. Front end

- **Monaco, trimmed.** A build of `monaco-editor` with the editor core and the C# Monarch grammar only, without the
  TypeScript, JSON, CSS and HTML language services. Built once by a small script and committed as embedded resources
  under `Viewer/monaco/`, with its version and license recorded next to it. Expected size: 2 to 4 MB in the package.
- **Served as assets.** `GET <token>/assets/<path>` serves the embedded files, with the same token and `Host` checks as
  every route, a fixed content type per extension, and `Cache-Control: private, max-age=86400, immutable` under a
  versioned path (`assets/<hash>/…`), so a page does not download the editor again. Unknown paths are 404; there is no
  directory listing and no file system access.
- **CSP of a served page.** Adds `script-src 'self'` (next to the hash of the viewer script), `font-src 'self'` (the
  codicon font), and `worker-src 'self'` (the editor worker). `style-src 'unsafe-inline'` is already there. A
  downloaded snapshot keeps today's policy, which loads nothing.
- **Loading.** The viewer script creates the plain box as today, then loads the editor. Once it is ready, the editor
  replaces the box with the same text; if it fails to load, the box stays. Ctrl+Enter and Run keep their meaning.
- **Language features.** The viewer registers a completion, hover and signature help provider for the `csharp`
  language, each calling the endpoints of section 3.2 with the code and the offset, and sets markers from
  `diagnostics`, debounced (about 400 ms after the last edit). Requests carry a sequence number, and stale answers are
  dropped.
- **Look.** The editor follows `prefers-color-scheme` (`vs` or `vs-dark`), grows with its content up to half the
  window as the box does, and has no minimap.

### 3.2. Endpoints

`POST <token>/complete`, `/hover`, `/signature` and `/diagnostics`, each with a JSON body
`{ "code": "…", "position": <offset>, "workspace": "…" }` (no position for diagnostics). They are protected exactly like
`/query`: the token, `Host`, `Origin` equal to the origin of the server, `Sec-Fetch-Site` absent or `same-origin`, an
`application/json` body of at most 1 MB, no CORS headers. The checks move to one method that all five routes call, so
that a new route cannot forget one.

Each forwards to a worker method of the same name over the existing pipe. The worker client serializes calls, so a
running query delays IntelliSense; to keep typing responsive, IntelliSense requests get their own short timeout (2
seconds) and the page drops an answer that comes late. Whether the worker should answer IntelliSense while a query runs
is left to measurement (section 5).

### 3.3. Worker

- **One script document per call.** The worker builds an `AdhocWorkspace` with a single project: the references of
  `QueryCompiler.GetReferences`, `LanguageVersion.Preview`, the compilation options of `QueryCompiler.Emit`, and one
  document, the source of `GenerateSource`. The workspace and project are built once and kept; each call only replaces
  the document text (`Solution.WithDocumentText`), so Roslyn reuses the compilation of the references.
- **Positions.** `GenerateSource` returns, with the source, the offset at which the code starts. A position in the
  editor maps to that offset plus the position; spans in answers map back by subtracting it, and anything outside the
  code is dropped.
- **Expression or statements.** The compiler tries the expression form first. IntelliSense uses the expression form when
  `TryGetExpression` accepts the code, and the statements form otherwise, so that `workspace.` (an incomplete
  expression) still completes as a statement and `x.Take( 3 )` gets the expression's context.
- **Completion.** `CompletionService.GetCompletionsAsync`, then `GetDescriptionAsync` for the selected item only
  (Monaco's `resolveCompletionItem`), so a list of 500 members does not compute 500 descriptions. Roslyn tags map to
  Monaco kinds (`Method` → `Method`, `Property` → `Property`, `Class` → `Class`, `Keyword` → `Keyword`, …). The
  replacement span comes from `CompletionItem.Span`. Members of `QueryBase` that are not globals (`ExecuteAsync`,
  `Equals`, …) are filtered out.
- **Quick info.** `QuickInfoService.GetQuickInfoAsync`, its sections joined as Markdown with the signature in a C#
  code block.
- **Signature help.** Roslyn's signature help is internal to `Microsoft.CodeAnalysis.Features`. Either use it through
  `Microsoft.CodeAnalysis.ExternalAccess.*` if one exposes it, or compute it from the semantic model: the enclosing
  `ArgumentListSyntax`, the candidate symbols of the invocation (`GetSymbolInfo(...).CandidateSymbols` or
  `GetMemberGroup`), and the index of the argument at the caret. The second is enough for methods and constructors,
  which is what queries call. To decide while implementing.
- **Diagnostics.** `GetDiagnostics()` of the semantic model, filtered to the code and mapped back, with the severities
  that Run reports. CS1002 at the end of an incomplete expression is expected while typing and shown like any other.
- **No user code runs.** These services parse and bind only. The source generators and analyzers of the workspace are
  not loaded into this project. A test asserts that a query with side effects in a static constructor or property getter
  has none during completion.

### 3.4. Security

The new routes run no user code, but they read the code model through Roslyn and the workspace references, so they are
protected like `/query` and not like the GET routes. Nothing changes for the agent, the MCP App view or a snapshot. The
assets route serves only embedded files. The design document gets a paragraph in section 4.1 and a section 9.7 that
points here.

## 4. Testing

- Unit tests of the position mapping and of the expression or statements choice, on the generated source.
- Worker tests: completion after `workspace.` contains `SourceCode`; after `workspace.SourceCode.Types.First().` contains
  `Methods`; quick info on `Methods` names `IMethodCollection`; signature help inside `Take( ` names the `count`
  parameter; diagnostics of `workspace.Typs` report CS1061 at column 11; a getter with a side effect does not run.
- End-to-end tests of the routes: the same origin, `Sec-Fetch-Site`, content type and token theory as `/query`, applied
  to each of them; an asset is served with its content type and the cache header; an unknown asset is 404; a served page
  has the new CSP and a snapshot the old one.
- A manual check in Edge with the development certificate: typing, completion, hover, signature help, markers, Ctrl+Enter,
  dark mode, and the fallback when the assets are blocked.

## 5. Open questions

- Whether IntelliSense should bypass the serialization of the worker client while a query runs, which needs a second
  channel or a reentrant worker. Measure first: most queries finish in well under a second.
- Whether the Monaco build is committed or produced by the build. Committing keeps the build offline and reproducible;
  a script keeps the repository small. The proposal is to commit it, as the viewer's other files are.
- Whether to show the result of `diagnostics` in the agent's text when Run fails. Today the compile error is already
  there.

## 6. Estimate

| Part | Effort |
|---|---|
| Monaco build, assets route, CSP, loading and fallback | 1 day |
| Worker: script workspace, completion, quick info, diagnostics | 1 day |
| Signature help, position mapping edge cases, tests | 0.5 to 1 day |
| Docs and manual verification | 0.5 day |

About 3 days in total, and the package grows by the Monaco build only.

## 7. Implementation notes

- **The Monaco build.** `Viewer/editor-build` bundles `monaco-editor` 0.57.0 with esbuild 0.28.2 from its ESM sources
  into `Viewer/editor` (committed): `editor.js` (3.8 MB), `editor.css`, `editor.worker.js` and `codicon.ttf`, about
  4.4 MB in all. The AMD build that the package also ships is deprecated and loads the TypeScript, JSON, CSS and HTML
  services eagerly, so it was not used. The entry imports `features/register.all` rather than a hand-picked list of
  contributions: with a hand-picked list, hover silently did nothing, and all the editor features cost only 0.6 MB more.
  No language service is included.
- **CSP.** A served page also needs `style-src 'self'` for the style sheet of the editor, which the plan missed. The
  bundle uses no `eval`.
- **Routes.** `complete`, `describe` (the details of one completion item, from the last list), `hover`, `signature`
  and `diagnostics`. The editor files are at `<token>/assets/<hash>/<name>`, with the hash of the four files; any
  other name or hash is 404.
- **Timeout.** IntelliSense calls use a soft timeout of 60 seconds, not 2: the worker is stopped at twice the soft
  timeout, and the first call loads the language services of Roslyn. A 2-second timeout would have killed the worker,
  and with it the loaded workspaces. The page drops answers that come late instead.
- **Signature help** is computed from the semantic model (`GetMemberGroup` of the invoked expression, or the
  constructors of the created type, and the commas before the caret), because Roslyn's service is internal.
- **Documentation.** References whose XML documentation file sits next to the assembly get it, so quick info and
  completion details show summaries where the file ships.
- **Tests.** `QueryEditorEndToEndTests` covers completion (members, elements of a collection, globals without the
  generated class), completion details, hover, signature help, diagnostics (an error at its column, and no false
  positive for expressions, `void` calls and statements), that no code runs, the security checks on every route, the
  editor files and their cache header, the CSP, and the snapshot without editor.
