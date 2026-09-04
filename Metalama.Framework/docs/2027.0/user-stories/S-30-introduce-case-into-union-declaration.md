### S-30. C# 15 unions: introducing a case into a `union` declaration

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Priority: nice to have for 2027.0. It is discretionary under the doctrine of section 12 of
  [`DECISIONS.md`](../DECISIONS.md) and slips to 2027.1 if the release runs short.
- Blocked by: S-29, and question Q1 of [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md)
- Findings: none. The design is
  [`analysis-reports/11-introducing-unions-design.md`](../analysis-reports/11-introducing-unions-design.md).

---

This story is filed only if question Q1 chooses Option A, which is to ship both authoring forms of case
introduction. It adds a case to a type declared with the `union` keyword, which S-29 leaves out because that
operation works at build time only.

#### Context

Exactly one part of a partial union carries the case list, a second one is `CS8863`, and none is `CS9370`, as
[`analysis-reports/11-introducing-unions-design.md`](../analysis-reports/11-introducing-unions-design.md) settles. A
generated partial part can therefore never add a case to a union declared with the `union` keyword. The operation
has to rewrite the part the user wrote, which the linker can do at build time and which the design-time pipeline
cannot do at all, so the editor and the build disagree and the divergence has to be reported rather than repaired.

#### Scope

- Add the case introduction for a `union` declaration, with the linker rewriting the case list of the part the user
  wrote.
- Report a design-time diagnostic stating that the editor cannot show the added case.
- Write the aspect tests with their committed baselines, including the design-time scenario.
- State, in the pull request description, which pages of `metalama/Metalama.Documentation` must follow, including the
  note that an added case is a build-time-only change.

#### Acceptance criteria

- An aspect adds a case to a `union` declaration, and the build result is correct.
- A design-time diagnostic states that the editor cannot show the added case.
- The aspect tests and their expected output are committed, and the pull request description explains each difference
  from the previous baseline.

— Claude for @gfraiteur
