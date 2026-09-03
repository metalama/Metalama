# TemplateLanguageVersion14

Asserts that a template may be written in C# 14. See issue #1896.

The accessors of the introduced property in `Test.cs` use the `field` keyword of C# 14. The accessors of an
introduced member are templates, so the template compiler verifies them against `MetalamaTemplateLanguageVersion`,
which `Directory.Build.props` sets for the whole repository. The `field` keyword is represented by a syntax node
that Roslyn 5.0 added, so the verification reports `LAMA0232`, "Template code must be written in C# 13.0", while
that property is `13.0`.

There is no `test.json`: the assertion is that the scenario builds and runs cleanly, which it could not do while the
property was pinned to `13.0`. The project sets `LangVersion` to `14.0` of its own, so that the source language
version is not what the scenario measures and the assertion is about the template language version alone.

The value that this scenario guards is bounded by the lowest Roslyn version that a supported host presents, because
a template is compiled by the Roslyn of the host. That version is `RoslynApiMinVersion` in `Directory.Packages.props`,
and the platform baseline that decides it is `Metalama.Framework/docs/platform-support.md`.
