### S-16. Code model: support for the closed modifier

- Issue type: User Story
- Labels: `enhancement`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: S-13, S-15
- Findings: [CM-5](../03-code-model-unions-closed.md)

---

A closed class breaks nothing today: it is a class with a modifier that the compiler merges across partial parts,
Roslyn adds the abstract flag to it so the code model reports `IsAbstract` correctly, and the derived-type model is
already right. Two things are missing: an aspect cannot tell that a class is closed, and the documentation does not
say that the enumeration of direct derived types is exhaustive for such a type.

#### Context

The language requires every subtype of a closed type to be in the same module, and the derived type index already
restricts itself to the current compilation, so the existing enumeration of direct derived types is already the
complete set for a closed type declared in the current compilation. Only the flag and its documentation are new. The
one case the index does not answer is a closed type that comes from a referenced assembly, which is question Q8 and
is left open. The read names a Roslyn member that the lower variant does not expose, so it follows the gating of
S-13, and the emission of a `closed` modifier is delivered separately by S-28, which section 4 of
[`DECISIONS.md`](../DECISIONS.md) puts in scope.

#### Scope

- Expose on `INamedType` whether a named type is closed, following the precedent of the other type flags, and gate
  the read to the latest Roslyn variant.
- Document in the derived-type options that, for a closed type declared in the current compilation, the direct
  enumeration is exhaustive.
- Record what the value is for a builder and for an introduced type, which is false until S-28 adds the writer.
- Record question Q8, the closed type read from a referenced assembly, as a known limitation in the documentation
  rather than implementing it.
- Pin with a test that the lower Roslyn variant answers false and reports no diagnostic, as S-18-1 does, so that the
  two readers behave alike.

#### Acceptance criteria

- An aspect can tell a closed class from an ordinary abstract class, and the value is false on the lower Roslyn
  variant.
- The documentation of the derived-type options states the exhaustiveness rule and its condition.
- Both Roslyn variants build.

#### Not in scope

This story does not introduce a closed class, which is S-28. It delivers the reader that S-28 consumes.

— Claude for @gfraiteur
