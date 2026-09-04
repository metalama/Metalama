### S-18-6. C# 15 unions: pattern and extension libraries

- Issue type: User Story
- Labels: `enhancement`, `Area-Patterns`, `Area-Extensions`, `Area-Framework`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Blocked by: S-18-1
- Findings: [UT-14](../06-user-tfm-patterns-tests-docs.md), [UT-14a](../06-user-tfm-patterns-tests-docs.md),
  [UT-14b](../06-user-tfm-patterns-tests-docs.md), [UT-14c](../06-user-tfm-patterns-tests-docs.md),
  [UT-14d](../06-user-tfm-patterns-tests-docs.md)

---

A union is an ordinary struct with an opaque value property for every library that reads the code model, and that
premise has four consequences in the pattern and extension libraries plus one in the reference graph. Two of them are
product defects: the immutability classification calls a union mutable, which produces spurious observability
warnings, and the caching key generation and the cache item serialization treat a union as an opaque struct and
produce a silently wrong result.

#### Context

The immutability rule must key on the declaration form and not on the Roslyn union flag, because that flag is also
true for a hand-written type carrying the union attribute whose state is unconstrained. The caching defect has a
practical discriminator, which is the interface that the compiler makes every union implement, but the affected
projects target `net472` and `netstandard2.0` and cannot bind to it at compile time, so the check has to be made in
another way. The remaining three consequences are tests: the observability aspect already rejects a union with two
diagnostics and that behaviour is pinned rather than changed, and the multicast selector is correct for every target
except the implicit parameterless constructor, where materializing an override produces a compiler error. The
reference graph item needs one override in the core reference index walker, without which a reference from a union to
its case types is never attributed and an architecture rule under-reports with no diagnostic; the Premium half is two
architecture rule tests, which is why they are story S-23, and that story waits for S-14, because Premium cannot
compile a union until its own latest variant is renamed.

#### Scope

- Classify a union declaration in the immutability library as shallowly immutable, and as deeply immutable when every
  case type is, treating an interface, a type parameter, a nullable value type and a nested union case type
  conservatively, and document the rule against the definition the library gives rather than against the `readonly`
  modifier.
- Make the caching key generation and the cache item serialization treat a union by its case value rather than as an
  opaque struct, and choose a discriminator that the target frameworks of the affected projects can express.
- Add the observability test that pins the two diagnostics with which the library already rejects a union.
- Prevent the multicast selector from selecting the implicit parameterless constructor of a union, or make
  constructor advice ineligible on a union in the engine, and record which of the two was chosen.
- Attribute a reference from a union declaration to its case types in the core reference index walker, by entering
  the union type declaration as the current declaration before visiting its case list.

#### Acceptance criteria

- A union of immutable case types does not produce an observability warning, and a hand-written type carrying the
  union attribute is not classified from the attribute alone.
- A cached method whose parameter is a union produces a different cache key for two different cases, and a cached
  value of union type survives a serialization round trip.
- The multicast and observability tests are committed with their expected output.

#### Not in scope

This story does not add the architecture rule tests of `Metalama.Premium`, which are story S-23.

— Claude for @gfraiteur
