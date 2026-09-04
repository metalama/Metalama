### S-04. Design time: renaming a code refactoring provider disables it in the editor and no test detects it

- Issue type: Bug
- Labels: `bug`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: none. The defect was found by the review of the subsystems that no theme document examined, which is
  question Q14 of [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md).

---

`MetalamaCodeRefactoringProvider` names the assembly and the three implementation types that it loads as string
literals, at `Metalama.Framework/src/Metalama.Framework.EditorExtensions/MetalamaCodeRefactoringProvider.cs:34-35`,
`:42-43` and `:50-51`. Those three type names are absent from
`Metalama.Framework/src/Metalama.Framework.DesignTime/RoslynEntryPointTypeNames.cs:17-32`, and therefore also absent
from the test that compares each constant with the full name of the real type,
`Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/DesignTime/TestRoslynEntryPointTypeNames.cs:27-37`.
Renaming or moving `TheCodeRefactoringProvider`, `VsCodeRefactoringProvider` or `RiderCodeRefactoringProvider`
therefore compiles and passes the whole test suite, and the failure appears only when a user opens a file in an
editor.

#### Context

The three names are correct today, so this is a defect of the guard rather than a defect that a user can observe now.
The consequence of a stale name is worth stating, because it decides the severity. `ResourceExtractor.CreateInstance`
resolves the type with `assembly.GetType( typeName, true )` at
`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ResourceExtractor.cs:253-255`, writes a crash report
and rethrows at `:307`. The constructor of `MetalamaCodeRefactoringProvider` then throws, Roslyn fails to compose the
exported code refactoring provider, and every Metalama refactoring disappears from the editor. The only record is a
file in the crash report directory.

The sibling facade in the same directory does not have the problem.
`Metalama.Framework/src/Metalama.Framework.EditorExtensions/MetalamaCodeFixProvider.cs:36-37`, `:44-45` and `:52-53`
use the constants, and the eleven constants that exist are covered by the test. The two facades are written from the
same design and disagree only on this point.

The summary of `RoslynEntryPointTypeNames` at `RoslynEntryPointTypeNames.cs:9-12` states that the type lists the
public entry point types exposed to Roslyn, that the list is referenced by the `CompilerExtensions` and
`EditorExtensions` projects, and that it is unit tested. Neither the first nor the third statement is true of the
three code refactoring provider types. A reader who trusts the summary concludes that the entry points are covered
when they are not.

The release touches this area. Story S-13 renames the latest Roslyn variant, which changes the assembly identity
that `ResourceExtractor` composes at `ResourceExtractor.cs:244-246`, so the entry-point loading path is read and
edited during 2027.0 in any case.

#### Scope

- Add three constants to `Metalama.Framework/src/Metalama.Framework.DesignTime/RoslynEntryPointTypeNames.cs`, for
  `Metalama.Framework.DesignTime.CodeFixes.TheCodeRefactoringProvider`,
  `Metalama.Framework.DesignTime.VisualStudio.CodeFixes.VsCodeRefactoringProvider` and
  `Metalama.Framework.DesignTime.Rider.RiderCodeRefactoringProvider`.
- Replace the six string literals of
  `Metalama.Framework/src/Metalama.Framework.EditorExtensions/MetalamaCodeRefactoringProvider.cs:34-35`, `:42-43` and
  `:50-51` by those constants and by `RoslynEntryPointTypeNames.DesignTimeAssemblyName`, so that the file names no
  type and no assembly by a literal.
- Add one `InlineData` row per new constant to `TestRoslynEntryPointTypeNames.TestConstant` in
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/DesignTime/TestRoslynEntryPointTypeNames.cs`.
- Correct the summary of `RoslynEntryPointTypeNames` at `RoslynEntryPointTypeNames.cs:9-12`, so that what it claims
  about coverage is true after the change.
- Balance the diagnostic suppression at `MetalamaCodeRefactoringProvider.cs:58-62`, where `VSTHRD110` is disabled
  twice and restored once.

#### Acceptance criteria

- No source file of `Metalama.Framework.EditorExtensions` names an entry point type or the design-time assembly by a
  string literal.
- Every constant of `RoslynEntryPointTypeNames` is covered by a row of `TestRoslynEntryPointTypeNames`, and every
  entry point that `Metalama.Framework.EditorExtensions` and `Metalama.Framework.CompilerExtensions` load is a
  constant of `RoslynEntryPointTypeNames`.
- Renaming any one of the three code refactoring provider types makes the unit test suite fail.
- Both Roslyn variants build, and the projects that this story touches build with no warning under
  `-p:ContinuousIntegrationBuild=True`.

#### Not in scope

This story does not change how the process kind is detected, and it does not change which implementation each host
receives. The process kind detection is duplicated between
`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ProcessKindHelper.cs:14-59` and
`Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:34-139`, and the two copies have diverged,
but the divergence has no functional consequence here, because every switch over the process kind sends an
unrecognized host to a default arm that loads the general implementation.

— Claude for @gfraiteur
