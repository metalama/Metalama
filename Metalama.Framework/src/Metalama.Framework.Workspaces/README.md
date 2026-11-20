![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

## About

`Metalama.Framework.Workspaces` package loads the full Metalama pipeline for inspection, without compiling.

When loading and running Metalama using this package, you can get the object model of aspects, advices, transformations, diagnostics etc.

It also allows you to query code metrics.

## Requirements

The package targets `net10.0`. Before it loads a project, it registers the MSBuild of a .NET SDK in the current
process, and it chooses the highest SDK that the runtime of the process can load. The major version of the host
runtime therefore decides which SDK can be used: a process that runs on .NET 10 needs a .NET 10 SDK, and it can't
use a .NET 11 SDK, because the MSBuild of that SDK targets .NET 11. On a machine that has only the .NET 11 SDK,
install the .NET 10 SDK, or run your application on .NET 11.

## Principal APIs

- The [Workspace](https://doc.metalama.net/api/metalama-framework-workspaces-workspace) type is your entry point. It allows you to load a project or solution.

## Documentation

* Conceptual documentation: [Inspecting a project using LINQPad](https://doc.metalama.net/conceptual/introspection/linqpad)
* API documentation: [Introspection API](https://doc.metalama.net/api/introspection-api)

## AI Coding Assistants

If you use an AI coding assistant such as Claude Code, install the [Metalama AI Skills](https://github.com/metalama/Metalama.AI.Skills) plugin. It gives the assistant access to the Metalama conceptual documentation, the API reference, and code samples.
