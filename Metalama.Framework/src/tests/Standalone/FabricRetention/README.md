# Fabric retention diagnostic, enabled

End-to-end test of the opt-in diagnostic that reports the references held by fabrics that pin a Roslyn compilation
(issue [#1802](https://github.com/metalama/Metalama/issues/1802)).

## What this scenario covers, and what it does not

The detection logic is covered by unit tests: `ObjectGraphWalkerTests` for the walk,
`UserCodeRetentionPolicyTests` for what counts as a retention and to whom it is attributed, and
`UserCodeRetentionAnalyzerTests` for real fabrics driven through the compile-time pipeline. None of those goes through
MSBuild.

This scenario covers only what they cannot: that `MetalamaDiagnoseMemoryLeaks`, set as an ordinary MSBuild
property in the project file, actually reaches `IProjectOptions`. That path runs through a `CompilerVisibleProperty`
item in `Metalama.CompilerVisibleProperties.props`, the `.editorconfig` that Roslyn generates from it, and
`MSBuildProjectOptions`. A property that is implemented everywhere but forgotten in the props file would pass every
unit test and do nothing in a real build.

## The fabric

`LeakyFabric` accumulates every declaration its predicate visits into a field of its own. The predicate runs while the
query is executed, so the field is filled after `AmendProject` has returned, which is also why the analysis runs after
the pipeline rather than immediately after the fabrics.

## Expected outcome

The build **succeeds**, because the diagnostics are warnings, and emits:

- `LAMA0085`, once per pinned declaration, naming `LeakyFabric` and the chain of fields that reaches it;
- `LAMA0086`, the summary, naming the report file.

`FabricRetention.Disabled` is the companion scenario that asserts the same project produces neither diagnostic when
the property is not set.

## How to run

`Build.ps1 test`, which builds every scenario under `Standalone`. To reproduce by hand:

```
dotnet build FabricRetention.csproj
```
