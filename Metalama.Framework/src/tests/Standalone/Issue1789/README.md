# Issue1789

Asserts that `MetalamaCompileTimeTargetFrameworks` is read in full when it is written in the documented,
semicolon-separated form. See issue #1789.

The property reaches the compiler through the generated analyzer configuration file, in which a semicolon starts a
comment. Everything after the first semicolon was therefore dropped, `netstandard2.0;net10.0;net48` was read as
`netstandard2.0` alone, and the assembly locator then failed its own requirement of a .NET 6.0 or later target
framework and threw, which surfaced as `LAMA0001` and invited a crash report for what is a configuration value.

The build now normalizes the separator to a comma, as it already did for every other list-valued option, and the
engine accepts either separator, so that a value set directly through `IProjectOptions` (as in unit tests) keeps
working.

There is no `test.json`: the assertion is that the scenario builds and runs cleanly, which it could not do before the
fix. The value is deliberately the same set of target frameworks as the default, so that the scenario asserts that the
value is parsed in full without changing what is actually built. The parsing itself is covered by
`CompileTimeTargetFrameworksTests`, and an invalid value is now reported as `LAMA0084` rather than as a crash.
