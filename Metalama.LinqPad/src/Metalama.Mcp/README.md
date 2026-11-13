![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

`Metalama.Mcp` is a local [Model Context Protocol](https://modelcontextprotocol.io) server that lets coding agents explore C#
code. An agent loads a project or solution once and then runs C# queries over the Metalama code model and introspection API, as
a person does with the `Metalama.LinqPad` driver.

## Installation

Register the server with your MCP client. For Claude Code:

```
claude mcp add metalama -- dnx Metalama.Mcp
```

Or install it as a tool with `dotnet tool install -g Metalama.Mcp` and register the `metalama-mcp` command.

Options:

* `--output-dir <directory>`: where HTML results are written. Default: `<temp>/metalama-mcp`.
* `--timeout <seconds>`: the default soft timeout of a query. The worker process is stopped after twice this time.

## Tools

| Tool | Purpose |
|---|---|
| `load_workspace` | Loads projects or solutions and returns their scopes and the link base. |
| `reload_workspace` | Reloads a workspace after files were edited. |
| `unload_workspace` | Frees a workspace. |
| `query` | Runs a C# expression or statements and renders the value. |
| `open` | Renders the object that a link names. |

Every rendered object starts with a link that `open` accepts, for example `T:MyApp.Customer/Methods`. Results can be rendered as
text (the default), as JSON, or as a self-contained HTML page with property tables and grids for a person to open. Hosts that
support MCP Apps show the same viewer next to the conversation, with live drill-down.

## Security

The server runs arbitrary code by design, with the privileges of the user who started it. It is not a sandbox. Its only
protection is that it communicates over stdio, so only the process that started it can reach it: it never opens a listening
socket, and it has no network option.

Loading a workspace runs the repository's MSBuild targets and its Metalama aspects and fabrics at compile time. Only load code
you trust.

## Related packages

* `Metalama.LinqPad` exposes the same code model to LINQPad queries.
* `Metalama.Framework.Workspaces` loads projects and solutions without a tool.
