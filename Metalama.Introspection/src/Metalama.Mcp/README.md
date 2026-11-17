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

To view results in a browser, trust the ASP.NET Core development certificate once:

```
dotnet dev-certs https --trust
```

Options:

* `--no-http`: never start the HTTPS server that shows html results. The server then never listens on any port.
* `--timeout <seconds>`: the default soft timeout of a query. The worker process is stopped after twice this time.
* `--editor-link <template>`: the link with which an HTML page opens a source location, shown as an icon. `{path}` is the
  absolute path in URI form, such as `/C:/src/App.cs`, and `{line}` is the line. Default: `vscode://file{path}:{line}`.
  Use `none` to show locations as text.

## Tools

| Tool | Purpose |
|---|---|
| `load_workspace` | Loads projects or solutions and returns their scopes and the link base. |
| `reload_workspace` | Reloads a workspace after files were edited. |
| `unload_workspace` | Frees a workspace. |
| `query` | Runs a C# expression or statements and renders the value. |
| `open` | Renders the object that a link names. |

Every rendered object starts with a link that `open` accepts, for example `T:MyApp.Customer/Methods`. Results can be rendered as
text (the default), as JSON, or in the html format, which returns an `https://localhost` link to a page with property tables
and grids for a person to open. Every declaration in the page expands on demand, and a Save button downloads a snapshot. Hosts
that support MCP Apps show the same viewer next to the conversation, with live drill-down.

## Security

The server runs arbitrary code by design, with the privileges of the user who started it. It is not a sandbox. MCP goes over
stdio only, so only the process that started the server can run code in it.

The first html result, or the first result shown in the MCP App view, starts an HTTPS server in the supervisor process, which
never runs a query: it shows stored results and follows links, for the browser and for the MCP App view. It listens on
`127.0.0.1` and `::1` only, on a random port, and every URL starts with a random token that only the agent and the user
receive. It accepts GET requests for `localhost` only, which defeats DNS rebinding. Only the endpoint that follows links
answers other origins, because the MCP App view runs on the origin of the host. The worker process, which runs the queries, never listens.

Loading a workspace runs the repository's MSBuild targets and its Metalama aspects and fabrics at compile time. Only load code
you trust.

## Related packages

* `Metalama.LinqPad` exposes the same code model to LINQPad queries.
* `Metalama.Framework.Workspaces` loads projects and solutions without a tool.
