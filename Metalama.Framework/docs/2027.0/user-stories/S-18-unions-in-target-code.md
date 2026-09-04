### S-18. C# 15 unions: support in target code

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L, which is the sum of the six sub-stories
- Blocked by: S-13 and S-15, which are the blockers of the first sub-story
- Findings: the findings of the six sub-stories, which are listed in each of them

---

An aspect applied to a union that a user wrote must work. This meta-story states that outcome and groups the six
stories that deliver it: the code model describes the union, the compile-time and design-time visitors and rewriters
handle it, the linker injects and links the advice applied to it, the design-time result matches the build, the
declaration comparers order and compare its members, and the pattern and extension libraries are correct on it. None
of the six is deferrable while C# 15 ships, because deferring one does not stop a user from writing a union.

#### Context

Section 12 of [`DECISIONS.md`](../DECISIONS.md) states the doctrine that decides the scope. Metalama must be able to
advise any user code: whatever a user writes, the code model must describe it, the pipeline must transform it, the
linker must emit it, and the design-time result must match the build. The same section classifies the reading and
advising half of the union work as required on that ground, and the introduction interface as discretionary. The
introduction of a union and of a case is therefore not part of this meta-story; it is S-29 and S-30.

The six sub-stories are one subject, and they are expected to be implemented as a stack of pull requests merged as
one. Each sub-story is a pull request of the stack, in the order below, so that each can be reviewed on its own,
and the stack is merged when the whole subject is complete. The reason is that the intermediate states are not
shippable: repairing the injection without the linking produces a worse state than repairing neither, and the code
model members that the later stories read do not exist until the first story adds them.

#### Sub-stories

- [S-18-1](S-18-1-union-in-code-model.md), the code model. It exposes the union on `INamedType`, adds the visitor
  overrides and adds the union kind to the type-declaration kind lists. Every other sub-story consumes it.
- [S-18-2](S-18-2-union-rewriters-and-visitors.md), the rewriters and visitors. It gives the compile-time code
  builder, the template annotator and the design-time text span classifier a dispatch for a union declaration.
- [S-18-3](S-18-3-union-linker-and-advising.md), the linker and advising. It injects, links and validates the advice
  applied to a union, and makes the members that the compiler synthesizes readable.
- [S-18-4](S-18-4-union-partial-part-design-time.md), the design-time partial part. It emits a union partial part
  instead of a struct partial part, which is the part of union support that a user sees first.
- [S-18-5](S-18-5-union-comparers.md), the declaration comparers. It repairs the aspect instance ordering and the
  conversion reimplementation that a union exposes.
- [S-18-6](S-18-6-unions-in-pattern-and-extension-libraries.md), the pattern and extension libraries. It corrects
  the immutability classification, the caching key generation and serialization, the multicast selector and the
  reference index walker.

#### Scope

- File the six sub-stories as sub-issues of this issue, in the order above.
- Implement them as a stack of pull requests, one per sub-story, and merge the stack as one.
- Keep the scope and the acceptance criteria of each sub-story in the sub-story, so that this issue states the
  outcome and not the work.

#### Acceptance criteria

- An aspect applied to a union that a user wrote produces code that compiles, or is refused with a Metalama
  diagnostic that names the language restriction.
- The design-time result of such an aspect matches the build-time result on the hosts that the latest Roslyn variant
  serves.
- Every acceptance criterion of the six sub-stories is met.

#### Not in scope

This meta-story does not introduce a union or a union case, which are S-29 and S-30 and which section 12 of
[`DECISIONS.md`](../DECISIONS.md) classifies as discretionary. It does not carry the closed hierarchy work, which is
S-16 and S-28.

— Claude for @gfraiteur
