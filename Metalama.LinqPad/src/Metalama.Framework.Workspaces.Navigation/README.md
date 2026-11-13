![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

The `Metalama.Framework.Workspaces.Navigation` package contains the infrastructure shared by `Metalama.LinqPad` and
`Metalama.Mcp` to display objects of the Metalama code model and introspection API:

* the facade walker, which decides which members of an object are shown and in which order;
* the result tree, which summarizes a value with depth and item limits;
* the links, which name an object by a stable anchor and a path of steps, and resolve it again.

This package is an implementation detail of these tools and has no stable public API.
