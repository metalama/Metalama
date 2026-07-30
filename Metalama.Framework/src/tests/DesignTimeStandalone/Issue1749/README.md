# Issue 1749, at design time — two references providing the same compile-time assembly

The same reference graph as `Standalone/Issue1749`, exercised through `Metalama.DesignTime.HostSimulator` instead of
`dotnet build`. The two scenarios assert different things and neither replaces the other.

| | Asserts |
|---|---|
| `Standalone/Issue1749` | the compile-time pipeline reports `LAMA0079` instead of failing with an unhandled `FileLoadException` |
| this one | the **design-time** pipeline survives the same configuration, which is asserted simply by the simulator exiting successfully |

## Why a separate design-time scenario is needed

The configuration is rejected at compile time, so one might expect the design-time side to need no attention. It
does, for two reasons.

The design-time pipeline reaches the condition through a **different code path and earlier**. It diffs the referenced
projects in `ProjectVersionProvider` before the compile-time project repository is built, so it hit the duplicate
before `LAMA0079` could ever be reported. `ProjectKey` is an assembly name plus a hash of the preprocessor symbols,
so two referenced projects that produce the same assembly name yield the same key, and a plain `Add` into an
`ImmutableDictionary` threw:

```
System.ArgumentException: An element with the same key but a different value already exists.
Key: 'Contract, 2c261a018ff9f98d'
   at ImmutableDictionary`2.Builder.Add(TKey key, TValue value)
   at ProjectVersionProvider.Implementation.GetProjectReferencesAsync(...)
```

That exception came out of the source generator, which stopped **all** design-time support for the consuming project:
no generated code, no diagnostics, and therefore not even the `LAMA0079` that tells the user what to fix. A user in
that state sees an editor that has silently given up.

The second reason is that at design time a project reference is a `CompilationReference`, whereas by compile time
every reference is a `PortableExecutableReference`. The two pipelines therefore see the reference graph through
different types, and a fix on one side says nothing about the other.

## What the fix does

The design-time diff now keeps the first reference for a given `ProjectKey` and logs the one it drops. Choosing the
first is arbitrary, but it is consistent with the change list built alongside it, and correctness is not at stake:
the configuration is already a compile-time error, so all the design-time pipeline has to do is stay alive long
enough for the user to read it.

## Running it by hand

```powershell
dotnet <path>\Metalama.DesignTime.HostSimulator.dll Issue1749.sln --timeout 240
```

Add `--trace "*"` for Metalama's own trace. To see it during an MSBuild build instead, all three of
`METALAMA_CONSOLE_TRACE`, `--disable-build-servers` and `-v:detailed` are required; see the repository `CLAUDE.md`.

## The two pipelines deliberately disagree here

After the fix, the design-time pipeline **succeeds** on this configuration and applies the aspects inherited from
both `Middle1` and `Middle2`, while the build **fails** with `LAMA0079`. That is intentional, but it is worth stating
plainly: the editor shows aspects that the build refuses to produce.

The alternative would be for the design-time pipeline to fail too, which is what it effectively did before the fix,
and which cost the user every design-time feature for the project including the diagnostics that explain why. Since
`LAMA0079` is an error, the build cannot succeed and the discrepancy cannot be missed for long. `LAMA0079` itself is
not reported at design time: a diagnostic produced on that path does not reach the editor, which is the subject of
issue #1758.
