### S-28. Add the union and closed architecture rule tests of `Metalama.Premium`

- Issue type: User Story
- Labels: `enhancement`, `Area-Extensions`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: S
- Blocked by: S-10, S-22
- Findings: [PR-12](../07-premium.md)

---

An architecture rule on a type that is used as a union case under-reports with no diagnostic, because the reference
from the union declaration to its case types is not attributed. S-22 attributes that reference in the core reference
index walker, and this story adds the tests that prove the result in `Metalama.Premium`.

#### Context

The tests must be compiled against a Roslyn that has the union syntax, so this story waits for S-10, which renumbers
the latest variant of `Metalama.Premium`. It is a separate story from S-22 because a pull request cannot span two
repositories.

#### Scope

- Add the two architecture rule tests in `Metalama.Premium`, one for a rule on a type used as a union case and one
  for a closed class.

#### Acceptance criteria

- An architecture rule on a type used as a union case reports the reference from the union.

— Claude for @gfraiteur
