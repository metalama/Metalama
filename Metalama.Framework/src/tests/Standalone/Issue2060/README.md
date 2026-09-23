# Issue2060

Regression test for [#2060](https://github.com/metalama/Metalama/issues/2060).

The `CreateMetalamaTouchFiles` target updated the timestamp of `MetalamaBuild.touch` on every build. This file is an
`AdditionalFiles` item, so it is an input of `CoreCompile`. As a result, `CoreCompile` ran on every build, even when
nothing had changed.

`Issue2060.proj` builds `Library/Issue2060.Library.csproj` twice. The second build has no change, so it must skip
`CoreCompile`. The test fails when the second build rewrites the intermediate assembly.

The test then deletes `MetalamaBuild.touch`, as the design-time pipeline does when it observes a source change. The
next build must create the file again and run `CoreCompile`.
