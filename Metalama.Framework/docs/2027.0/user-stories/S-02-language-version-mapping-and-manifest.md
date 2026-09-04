### S-02. Language version: the unsupported-version diagnostics crash instead of naming the version

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [LV-2](../01-language-version-and-hosts.md), [LV-4](../01-language-version-and-hosts.md),
  [LV-5](../01-language-version-and-hosts.md)

---

`LanguageVersionExtensions.ToDisplayStringSafe` maps the numeric values 1300 and 1400 by a numeric cast and has no
arm for 1500, so it throws `ArgumentOutOfRangeException` while formatting `LAMA0051` or `LAMA0052` and the user sees
`LAMA0001` with a request to open a support ticket instead of the designed diagnostic. In the same area,
`CompileTimeProjectManifest.ResolvedLanguageVersion` documents C# 13, is read by nothing, and disagrees with the two
live fallbacks at `CompileTimeCompilationBuilder.cs:1355` and `CompileTimeProjectRepository.Builder.cs:596`.

#### Context

These three items become dangerous only after S-15 raises the supported language version, and all three are cheaper
to fix before it. The display mapping is a numeric cast and therefore compiles against the Roslyn 5.0 variant as
well, so it needs no variant branch. The manifest question has a recorded precedent: #1185 reported the failure of a
compile-time project produced by a higher Roslyn version and read by a lower one, with the Roslyn error `CS8192`,
which is exactly what an aspect library compiled at C# 15 and consumed under the Roslyn 5.0 variant would produce
again. #1142 is the reason the value is serialized as an integer, and that must not change.

#### Scope

- Add the arm for the numeric value 1500 to `LanguageVersionExtensions.ToDisplayStringSafe:33-39`, and give the
  method a formatted fallback for an unknown value so that it never throws.
- Either delete `CompileTimeProjectManifest.ResolvedLanguageVersion` or route both fallbacks through it with the
  value the comment documents, so that the manifest has one answer rather than three.
- Add, at the reading side, a clamp of the manifest language version to the maximum that the running variant
  accepts, and a warning that names both versions, so that a library compiled at a higher language version degrades
  instead of failing with a compiler error.
- Format the unknown value numerically in that warning, or add the display arm first, so that the diagnostic path
  itself cannot throw.

#### Acceptance criteria

- Requesting an unsupported language version reports `LAMA0051` or `LAMA0052` with the version named, and never
  `LAMA0001`.
- A compile-time project manifest that carries no language version resolves to one documented value, and the two
  reading sites agree with it.
- A manifest that carries a language version above what the running Roslyn variant accepts produces a named warning
  and a clamped parse, and not `CS8192`.
- The change compiles for both Roslyn variants with no variant branch.

— Claude for @gfraiteur
