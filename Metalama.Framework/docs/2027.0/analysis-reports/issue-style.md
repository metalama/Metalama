# Style of user stories in metalama/Metalama (two recent examples, verbatim)

Issue type "User Story"; labels such as enhancement, Area-Framework, Area-Build-Engineering, breaking, agent; milestone such as 2027.0.1-preview. Every issue ends with the signature line "— Claude for @gfraiteur" (this is the one place where an em dash is used, as part of the fixed signature).

## Example 1: #1881 "Support Roslyn 5.10 and remove obsolete Roslyn version-specific builds and symbols"

## User story

As a Metalama developer, I want the framework to build against Roslyn 5.10 and to keep only the Roslyn version-specific builds and preprocessor symbols that are still used, so that Metalama supports the compiler version that `Metalama.Compiler` already targets and the build no longer maintains dead variants.

## Context

`Metalama.Compiler` on `develop/2027.0` already targets Roslyn 5.10. It sets `RoslynVersion` to `5.10.0` and pins `MicrosoftCodeAnalysisPackageVersion` to the preview build `5.10.0-1.26365.3`. This was merged in metalama/Metalama.Compiler#210.

`Metalama` still declares `RoslynApiMaxVersion` and `RoslynMaxVersion` as `5.0.0` in `Directory.Packages.props`, and its latest variant `eng/RoslynVersions/Roslyn.5.0.0.props` derives `ThisRoslynVersion` from `RoslynApiMaxVersion`. The two repositories are therefore on different Roslyn versions.

## Part 1: Support Roslyn 5.10

Roslyn 5.10 is not published on nuget.org. Version `5.10.0-1.26365.3` is a preview and must be restored from the `roslyn-consolidated` feed, which caches the Microsoft feeds.

Tasks:

- Restore the consolidated feed and its package source mapping in `nuget.base.config`, which is currently empty. ...
- Raise `RoslynApiMaxVersion` and `RoslynMaxVersion` to the Roslyn 5.10 package version in `Directory.Packages.props`.
- Decide whether the latest variant keeps the identity `5.0.0` ... Record the decision in `Directory.Packages.md`.
- Mirror every version change into `Metalama.Premium`, which has its own `Directory.Packages.props` and its own `eng/RoslynVersions` props files.

## Part 2: Remove obsolete Roslyn version-specific builds and symbols

(A table of symbols, `#if` site counts and actions; a paragraph explaining why a variant must be kept; an "Optional" subsection with the two conditions that gate a further change.)

## Acceptance criteria

- `Metalama` and `Metalama.Premium` build and pass their tests against both Roslyn 5.10 and Roslyn 4.12.
- The `roslyn-consolidated` feed restores the preview Roslyn packages, and package source mapping limits that feed to `Microsoft.CodeAnalysis.*`.
- No preprocessor symbol defined by the variant props files is unused, and no symbol is defined by every variant.
- `Directory.Packages.md` describes the current variant coverage and the current floors.

— Claude for @gfraiteur

## Example 2: #1896 "Raise the template language version to C# 14 once the Roslyn floor is 5.0"

`Directory.Build.props` pins `MetalamaTemplateLanguageVersion` to `13.0`, under this comment: (quoted)

The stated reason no longer holds. The platform support doctrine in `Metalama.Framework/docs/platform-support.md` excludes Visual Studio 2022 from PB-2027.0: ...

## The real constraint is the Roslyn floor, not the Visual Studio version

A template is compiled by the Roslyn of the host, so the language version we may use in templates is bounded by the lowest Roslyn in the supported set, not by a Visual Studio version directly. C# 14 requires Roslyn 5.0. ...

## Scope

- Raise `MetalamaTemplateLanguageVersion` to `14.0` in `Directory.Build.props`, and rewrite the comment to name the Roslyn floor rather than a Visual Studio version, so that the next person to read it knows which number to check.
- Confirm that the aspect, template and linker test suites are green on both payload variants, since the template compilation is what changes.
- Check whether `Metalama.Extensions` and `Metalama.Patterns`, which the current comment names, carry any separate language-version pin of their own.

## Blocked by

#1881. Raising the value while the `Roslyn.4.12.0` variant still exists would break that variant, because Roslyn 4.12 does not support C# 14.

## Not to be confused with

`LangVersion` for our own product sources, which is a separate property and a separate decision. ...

— Claude for @gfraiteur

# Observed conventions

- Title: an imperative or descriptive sentence naming the mechanism, no ticket prefix.
- Body: opens with the concrete fact (a file, a property, a value), then the reasoning, then Scope as a bullet list of tasks naming files and properties, then Acceptance criteria as verifiable statements, then Blocked by, then optional Not in scope / Not to be confused with.
- Prose: complete sentences, no contractions, no figurative language, no bold emphasis inside paragraphs, file and property names in backticks, issue references as #NNNN or owner/repo#NNNN.
- Cite the platform baseline by name (PB-2027.0) and the doctrine documents (platform-support.md, Directory.Packages.md, updating-roslyn.md) rather than restating them.
