![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

The `Metalama.Testing.Hooks` package is not meant to be referenced in user projects.

It defines the test-only hooks that Metalama exposes from its production code, namely synchronization points and
fault injection points. These services are never registered in production, so a hook costs a null check.

No dependency of this package flows to its consumers, because it is referenced by production assemblies of every layer of Metalama.
