# Issue2060

Regression test for [#2060](https://github.com/metalama/Metalama/issues/2060).

The `CreateMetalamaTouchFiles` target updates the timestamp of `MetalamaBuild.touch` on every build, to signal the
design-time pipeline that a build has started. The file was registered as an `AdditionalFiles` item, so it was an input
of `CoreCompile`. As a result, `CoreCompile` ran on every build, even when nothing had changed.

The file is no longer an `AdditionalFiles` item. `Issue2060.proj` builds `Library/Issue2060.Library.csproj` twice. The
second build has no change. It must update `MetalamaBuild.touch`, and it must skip `CoreCompile`. The test fails when
the second build does not update the touch file, or when it rewrites the intermediate assembly.
