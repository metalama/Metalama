# Issue 2235 — an options type of an extension assembly in the aspect test runner

`Issue2235.Options` defines a hierarchical options type and an attribute that provides it. It is built with
`MetalamaEnabled=false`, as the public assembly of a Metalama extension package is. `Issue2235.Tests` is an aspect
test project that references it in three ways, as a project that references an extension package does:

- as a run-time reference, so the assembly is also in the output directory of the test runner;
- as a `MetalamaCompileTimeAssembly`;
- as a `MetalamaExtensionAssembly`, so the assembly is loaded into the compile-time domain.

The aspect of the test reads the options with `GetOptions<LabelOptions>()`. Before the fix, the pipeline created the
options from the copy of the assembly in the default `AssemblyLoadContext` of the test runner, and the aspect, which
uses the copy in the compile-time domain, failed with an `InvalidCastException`. In the `Metalama.Extensions.Interceptors`
package, the same defect made `[ExcludeInterceptors]` have no effect in an aspect test.

A unit test cannot reproduce the defect, because it requires the assembly to be both a dependency of the test process
and an extension assembly of the tested project.
