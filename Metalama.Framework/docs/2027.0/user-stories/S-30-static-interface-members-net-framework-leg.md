### S-30. Cover non-virtual static interface members on the .NET Framework test leg

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: S-11
- Findings: the section "Static members in interfaces without runtime support for default interface implementation" of
  [`08-roslyn-api-delta.md`](../08-roslyn-api-delta.md). The feature is also named by
  [UT-19](../06-user-tfm-patterns-tests-docs.md), which story S-11 owns.

---

`Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14`
declares the target frameworks `net48;net10.0`. Twenty of the thirty-five test input files of
`Tests/Aspects/Introductions/Interfaces` carry `@RequiredConstant(NET6_0_OR_GREATER)` and are therefore skipped on the
`net48` leg, and not one of the thirty-five introduces a non-virtual static member into an interface. C# 15 makes such a
member legal on a runtime that does not support default interface implementations, and .NET Framework is that runtime.
This story adds the tests that prove it. It needs no product change.

#### Context

The feature adds no syntax. It is Roslyn pull request 83097, merged on 2026-04-10 into the branch `features/Unions`,
and it has no language design proposal. A code search over `dotnet/roslyn` finds `IDS_FeatureStaticMembersInInterfaces`
in two files only, `src/Compilers/CSharp/Portable/Errors/MessageID.cs` and
`src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberMethodSymbol.cs`, and in no parser file.
`MessageID.RequiredVersion` returns `LanguageVersion.CSharp15` for it, in the same group as the five other C# 15
features. The whole of the change is the method
`SourceMemberMethodSymbol.ReportLackOfRuntimeSupportForStaticMembersInInterfaces`, which is called from
`SourceMemberMethodSymbol`, `SourceMemberFieldSymbol` and `SourceFieldLikeEventSymbol` when
`ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation` is false. For a protected, protected internal or
private protected member it reports `ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember`. For every other
accessibility it performs the language version check instead of reporting
`ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation`. A static abstract or static virtual member keeps
`ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces`, and an instance member with a body keeps its previous
error. Because there is no new syntax and no new Roslyn application programming interface member, neither the syntax
model regeneration of S-09 nor the variant gating of S-02 is involved.

Two compilations of Metalama lack the runtime support and are therefore the ones the feature changes. The first is the
test compilation of the `net48` leg: `GetMetadataReferences` in
`Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContext.CreateRoslynCompilation.cs:84-130` builds the
references from the assemblies of the running process, so on that leg the test compilation references the .NET
Framework 4.8 assemblies. The second is the compile-time compilation, which is always compiled against the
`netstandard2.0` reference set, as `CompileTimeAssemblyLocator.cs:664` and the validation at `:219-224` show, and whose
language version is the one returned by `ILanguageVersionProvider.GetCompileTimeLanguageVersion` at
`CompileTimeCompilationBuilder.cs:279`. Compile-time code that declares a static member with a body in a compile-time
interface is therefore refused today even in a project that targets `net10.0`.

Nothing in Metalama refuses a static interface member. No source of `Metalama.Framework/src` names
`RuntimeSupportsDefaultInterfaceImplementation`, and the eligibility rule `_introduceRule` of
`Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:117-126`, which serves every
introduction advice, accepts an interface target. `IntroduceMemberAdvice.InitializeBuilder` at
`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceMemberAdvice.cs:128-133` makes an
introduced member virtual only when the template is virtual or when there is no template, so a static template
introduced into an interface produces a non-virtual static member, and
`ModifierHelper.GetMemberSyntaxModifierList` at
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:122-141` then emits `static` and
neither `abstract` nor `virtual`. Metalama already generates the declaration that the feature legalises, and that
generated code is refused today when the target framework is `net472`, `net48` or `netstandard2.0`. The single
Metalama refusal in this area is `LAMA0534`, reported by `IntroduceFieldAdvice.ValidateBuilder` at
`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceFieldAdvice.cs:64-76` for every
field introduced into an interface, including a static one. That restriction predates C# 15, because a static field in
an interface has been legal since C# 8 on a runtime that supports default interface implementations, so narrowing it is
a separate decision and not a consequence of this feature.

The test mechanism that this story needs already exists and has no user. `TestOptions.TargetFrameworks`, documented at
`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:284-289` and evaluated at
`Metalama.Framework/src/Metalama.Testing.AspectTesting/TestInput.cs:96-116`, skips a test whose current target
framework is not in the requested set, and no test file uses the `@TargetFrameworks` directive today. Two properties
keep this story small. The expected output files are excluded from the compilation of the test project by
`Metalama.Testing.AspectTesting.targets:17-26`, and the feature adds no syntax, so the test inputs compile at the
pinned C# 14 language version of the test project on both legs. Neither the companion-file handling nor the language
version override that other C# 15 test directories need applies here.

#### Scope

- Create the directory `Tests/Aspects/CSharp15/StaticInterfaceMembers` in
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests`, following the conventions that S-11 establishes,
  which are the required constant of the renumbered Roslyn variant in the `metalamaTests.json` of the `CSharp15`
  directory and the `@LanguageVersion(15.0)` directive in each file.
- Add the tests that introduce a public non-virtual static method, a public non-virtual static property and a public
  non-virtual static event into an interface introduced by the aspect, modelled on
  `Tests/Aspects/Introductions/Interfaces/IntroduceMethodStaticVirtual.cs`,
  `IntroducePropertyStaticVirtual.cs` and `IntroduceEventStaticVirtual.cs`, but without the
  `@RequiredConstant(NET6_0_OR_GREATER)` directive and with a `@TargetFrameworks` directive that names both legs. Run
  each test and commit its expected output.
- Add one test that introduces a non-virtual static method into an interface declared in the test source, which is the
  scenario an aspect user meets, and one test with a private static method, whose accessibility the compiler admits
  because `ReportLackOfRuntimeSupportForStaticMembersInInterfaces` treats every accessibility other than the three
  protected ones alike.
- Add one negative test for a protected static member introduced into an interface, whose expected output records
  `ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember` on the `net48` leg.
- Add one compile-time test that declares a static member with a body in a compile-time interface, so that the
  `netstandard2.0` compile-time compilation is covered as well as the `net48` test compilation.
- Record in the issue, for each of the twenty gated files of `Tests/Aspects/Introductions/Interfaces`, why its
  `@RequiredConstant(NET6_0_OR_GREATER)` directive is kept: a static abstract or static virtual member still requires
  runtime support for static abstract interface members, and a private or virtual instance member with a body still
  requires runtime support for default interface implementations.
- Determine whether `LAMA0534` should be narrowed so that a static field is accepted, record the answer in the issue,
  and file a separate issue if the answer is that it should.

#### Acceptance criteria

- On the `net48` leg, an aspect that introduces a public non-virtual static method into an interface produces
  transformed code that compiles, and the test asserts that output instead of being skipped.
- The same test on the `net10.0` leg produces the same expected output file.
- A compile-time interface with a static member that has a body is accepted by the compile-time compilation, which
  targets `netstandard2.0`.
- A protected static member introduced into an interface is reported with
  `ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember` on the `net48` leg, and the expected output records it.
- No file of `Tests/Aspects/Introductions/Interfaces` loses its `@RequiredConstant(NET6_0_OR_GREATER)` directive, and
  the reason is written in the issue.
- The `@TargetFrameworks` directive has at least one user, and a test whose requested target frameworks exclude the
  current one is skipped with the reason visible in the test output.
- The answer about `LAMA0534` is recorded, whether it is a change or a decision to keep the restriction.

#### Not in scope

This story does not narrow `LAMA0534` and does not otherwise change the advice code. It does not cover static abstract
and static virtual interface members, which C# 15 leaves unchanged and which still require the runtime support that
.NET Framework does not provide. It does not use the feature in the sources of Metalama itself, which is impossible in
any case, because `Metalama.Framework` targets `netstandard2.0`, `Metalama.Framework.Engine` targets `net472`, and the
repository pins its own language version at `Metalama.Framework/Directory.Build.props:45-46`.

— Claude for @gfraiteur
