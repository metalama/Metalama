# Issue2249

A project that defines aspects and fabrics embeds `Metalama.CompileTimeProject.zip` as a managed resource, both in the
implementation assembly and in the reference assembly. The zip stores the path of each compile-time source file in
`manifest.json`, in the `OriginalPath` attribute of fabrics, and in the source hash. When these paths are not mapped
with `PathMap`, the same source built in two directories gives two different assemblies.

`AspectLib` maps its project directory to `/_/` with `PathMap`. `Issue2249.proj` copies the sources of `AspectLib` to
`obj/Copy/Deeper/AspectLib`, builds the project in both directories, and fails if the reference assembly or the
implementation assembly differs between the two builds.

See https://github.com/metalama/Metalama/issues/2249.
