### S-24. Clean up the `Metalama.Premium` build-file residuals

- Issue type: Bug
- Labels: `bug`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: S
- Blocked by: metalama/Metalama.Premium#84, whose branch this story is based on
- Findings: [PR-3](../07-premium.md), [PR-5](../07-premium.md), [PR-6](../07-premium.md), [PR-7](../07-premium.md)

---

metalama/Metalama.Premium#85, which closed the issue #1913, and metalama/Metalama.Premium#86 aligned Premium with
PB-2027.0 and moved its build image to the Visual Studio 2026 build tools. Four residuals remain, all of them small,
independent of every gate and confined to build files: obsolete
container components, a template language version pinned below what the repository now supports, package pins and
comments that state a rule with the wrong value, and a pair of central package entries that makes an intended client
update inert.

#### Context

The .NET 8 SDK and the .NET 6 runtime components of the container were justified by target frameworks that Premium no
longer has, and a stale Visual Studio 2022 channel manifest is still at the top of the Docker context.
`MetalamaTemplateLanguageVersion` is `13.0` under a comment that names a Visual Studio version, whereas the value is
bounded by the lowest Roslyn variant of the repository, which is now 5.0.0 and supports C# 14; raising it is expected
to change which system-type polyfills the compile-time compilation embeds. The `Microsoft.Build` pins do not follow
the core doctrine, and the licensing build task must move off its older target framework in the same change, because
the newer package has no compile asset for it. The two contradictory `StackExchange.Redis` entries mean the intended
version never takes effect.

#### Scope

- Remove the .NET 8 SDK and the .NET 6 runtime components from `eng/src/Program.cs`, remove the stale Visual Studio
  channel manifest from the Docker context, and decide whether the prerelease flag of the SDK version follows the
  core repository.
- Raise `MetalamaTemplateLanguageVersion` from `13.0` to `14.0`, mirroring the closed issue #1896 in the core
  repository, and rewrite its comment to name the lowest Roslyn variant of the repository rather than a Visual Studio
  version.
- Align the `Microsoft.Build` pins with the core doctrine, together with the move of the licensing build task off its
  older target framework, delete the dead property and the entry that no project references, and correct the
  rationale comments.
- Resolve the contradictory pair of `StackExchange.Redis` entries into one, and confirm the resolved version in the
  restored assets file.

#### Acceptance criteria

- The container installs no component whose reason has been removed, and the Docker context carries no manifest for
  an unsupported Visual Studio.
- The template language version of Premium equals the value the core repository uses, and its comment names the
  Roslyn floor.
- Every package pin of Premium that mirrors a core pin has the same value, and the comment states the rule the core
  doctrine states.
- The Redis client resolves to the intended version.

— Claude for @gfraiteur
