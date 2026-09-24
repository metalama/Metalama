# Issue2061

A project that defines aspects embeds `Metalama.CompileTimeProject.zip` as a managed resource, both in the
implementation assembly and in the reference assembly. When the zip entries store the current time, the reference
assembly changes on each build, and every referencing project recompiles.

`Issue2061.proj` rebuilds `AspectLib` twice, 3 seconds apart, and fails if the reference assembly or the
implementation assembly differs between the two builds.

See https://github.com/metalama/Metalama/issues/2061.
