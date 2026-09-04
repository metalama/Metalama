### S-25. Bring the platform, dependency and extensibility documentation up to the shipped 2027.0 state

- Issue type: User Story
- Labels: `enhancement`, `documentation`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: S-09, S-11
- Findings: [UT-18](../06-user-tfm-patterns-tests-docs.md), [LV-10](../01-language-version-and-hosts.md),
  [DT-9](../05-design-time-workspaces-linqpad.md), [PR-9](../07-premium.md), [PR-14](../07-premium.md)

---

Several documents and project comments still state the previous baseline: they name `net8.0` or C# 14 as the latest,
they describe a check that has since been disabled, and they do not name the repositories outside `metalama/Metalama`
that repeat the Core flavour literal. Five findings propose prose only, and four of them edit the same two documents,
which is why they are one pull request.

#### Context

The story is deliberately last, because a document written before the code is a second thing to correct. Two
corrections come from decisions rather than from the theme documents: the statement that the November 2026 long-term
servicing baseline carries a Roslyn version near 5.11 should name 5.12, and the variant table row that offers a
measured version of 5.10 or above should read 5.12 or above, both recorded as question Q10; and the statement that
the `net10.0` toolset rolls forward to .NET 11 overstates the mechanism, because the roll-forward selects .NET 11
only when no .NET 10 runtime is installed, which is question Q11. Both belong to the product owner, so this story
applies them only once they are approved.

#### Scope

- Correct the locations that still name `net8.0` or C# 14 as the latest, and the standalone project comments that
  describe a check that has since been disabled, including the comment of the default language version scenario.
- State that the remaining `net10.0` literals name the outputs of this repository and move only with the embedded
  Core flavour, so that they are not confused with the .NET SDK pin.
- State in the `Metalama.Framework.Workspaces` and `Metalama.LinqPad` package documentation that the host runtime
  major decides which .NET SDK the in-process MSBuild registration may use, so that a user on a machine carrying only
  the newer SDK understands the failure.
- Add a section about `Metalama.Premium` beside the existing one about `Metalama.Compiler` in
  [`platform-support.md`](../../platform-support.md), listing the Premium build files that repeat the Core flavour
  literal, which is repeated in eight places with no test on the comparison, and the task directory selection of the
  licensing targets.
- State in the extensibility guide that the target framework metadata of an extension assembly is compared for string
  equality against the Core flavour name of the current platform baseline, so that a merely compatible value does not
  match.
- Apply the two corrections of question Q10 and the one of question Q11 once the product owner approves them.

#### Acceptance criteria

- No document or project comment names a target framework or a C# version that PB-2027.0 has dropped as the latest.
- [`platform-support.md`](../../platform-support.md) lists the drift points of `Metalama.Premium` as it lists those of
  `Metalama.Compiler`.
- The two introspection packages state the host runtime and the .NET SDK they require.
- The roll-forward statement names the condition under which the roll-forward happens.

#### Not in scope

This story does not rewrite the audience paragraph of [`Directory.Packages.md`](../../../../Directory.Packages.md),
which #1903 owns. It does not change the well-known source generator attribute list of `Metalama.Framework.props`,
which is functional and not documentation: an attribute that a newer framework adds and that the list omits changes
aspect eligibility for a generated partial member with no diagnostic, so it needs its own behavioural change and test
and must not be lost in a documentation review. It does not edit the conceptual documentation under
`../Metalama.Documentation/content`, which is a separate repository and therefore a separate pull request. This story
lists the pages that must follow, and it does not edit them.

— Claude for @gfraiteur
