### S-12. Metalama.Vsx: PB-2027.0 and the flowed dependency pins

- Issue type: User Story
- Labels: `enhancement`, `Area-Vsx`, `Area-Platforms`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Vsx`
- Size: L
- Blocked by: nothing. The story is calendar-gated by the November 2026 Visual Studio releases in the same way as
  S-11, and it is not gated by any story of this repository.
- Findings: none. No theme document of this analysis names `Metalama.Vsx`, which is what question Q13 of
  [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) records. The repository is not cloned in the session that produced this
  analysis, and its issue tracker `metalama/Metalama.Vsx.Public` was not reachable from that session either, so
  nothing in the product itself was verified. Every statement below about the extension is marked as an assumption.

---

Five package versions in this repository are pinned below what a measurement of this repository would allow, and they
are pinned because of Visual Studio Tools for Metalama. `Directory.Packages.props:179-182` pins `StreamJsonRpc` at
2.20.17, `System.IO.Pipelines` at 8.0.0, `System.Diagnostics.DiagnosticSource` at 6.0.1 and
`Microsoft.Bcl.AsyncInterfaces` at 8.0.0, and `Directory.Packages.props:132` holds `System.Threading.Tasks.Extensions`
at 4.5.4 with a comment naming the extension. `Directory.Packages.md:387` states the release condition of all five:
they become free to bump only after the extension migrates off direct consumption of
`Metalama.Framework.DesignTime.Rpc` and the older extension version leaves support. Neither half of that condition can
be established in this repository.

#### Context

The mechanism is recorded in `Directory.Packages.md:378`. `Metalama.Framework.DesignTime.Rpc` merges `StreamJsonRpc`
and `MessagePack` into itself, which the package references at
`Metalama.Framework/src/Metalama.Framework.DesignTime.Rpc/Metalama.Framework.DesignTime.Rpc.csproj:34-35` and the
`ILMerge` target at `:89-98` perform, and it flows `System.IO.Pipelines`, `System.Diagnostics.DiagnosticSource`,
`System.Collections.Immutable` and `Microsoft.Bcl.AsyncInterfaces` on purpose, at `:38-41`. The extension is deployed
separately and, according to `Directory.Packages.md:378`, sets `CentralPackageTransitivePinningEnabled` to true, so
those flowed dependencies are resolved on the machine of a user who has the extension installed to the versions the
extension pins rather than to the versions our package metadata requests. A bump that is internally consistent here
can therefore break an extension that is already installed.

The supported route away from that coupling exists and its Metalama half is delivered. The rule table of
[`cross-process-communication.md`](../../cross-process-communication.md), at lines 16 to 22, states that cross-process
traffic between different Metalama versions is not allowed and that cross-version traffic uses
`Metalama.Framework.DesignTime.Contracts`, whose types carry frozen `[Guid]` attributes and are unified by common
language runtime type equivalence. Issue #1605, "Fix StreamJsonRpc.ConnectionLostException between VSX 2026.0.x and
Metalama 2026.1.x by adding a version-invariant notification contract", was closed as completed on 2026-05-01, under
the milestone 2026.1.11-preview, and was merged by pull request #1612, "Notification contract split and cross-binding
fix". That pull request added the version-invariant notification subscription contract to
`Metalama.Framework.DesignTime.Contracts`, with frozen `[Guid]` markers, and registered the implementation through the
design-time entry point manager, so that a cross-version consumer no longer has to reference
`Metalama.Framework.DesignTime.Rpc`. The contract therefore exists, and this story neither designs nor builds it.

What remains is the consumption of that contract by the extension, and then the release of the flowed dependency
pins. The body of issue #1605 states that it pairs with `metalama/Metalama.Vsx.Public` issue 17, which is the Visual
Studio Tools side that consumes the new contract with a fallback to the previous path. The condition recorded at
`Directory.Packages.md:387` names issue #1605 and `metalama/Metalama.Vsx.Public` issue 18. Whether the consumption
has already happened is the fact that decides the size of this story, and it cannot be read from the session that
produced this analysis, because that repository was not reachable from it. The story therefore states as an
assumption that the consumption is still open, and it names issue 17 as the place to check.
`cross-process-communication.md:88` supports that assumption from this side: it records that the pipe-based delivery
of `ServiceHubServerEndpoint` is retained so that older extension builds that still consume
`Metalama.Framework.DesignTime.Rpc` keep working.

PB-2027.0 changes the other half of the condition. [`platform-support.md`](../../platform-support.md), at lines 124 to
134, removes Visual Studio 2022 from the supported set in its entirety and names the Visual Studio 2026 long-term
servicing channel baseline and Visual Studio 2027 as the two versions in the set. Two pins of this repository are
still derived from the Visual Studio 2022 floor of the extension rather than from ours:
`Metalama.Framework/src/Metalama.Framework.DesignTime.Contracts/Metalama.Framework.DesignTime.Contracts.csproj:32-33`
overrides `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Workspaces.Common` to 4.0.1, and the comment at
`:35` states that the `Newtonsoft.Json` reference must match the version used by the lowest version of Visual Studio
that the extension supports. Both are frozen on purpose, because that project is loaded side by side by every
Metalama version present in one Visual Studio session, which the comment at `:17-19` states. Its target frameworks are
`net472` and `net10.0`, at `:4`.

#### Scope

- State which Visual Studio versions Visual Studio Tools for Metalama supports for the 2027.0 release, and reconcile
  that set with PB-2027.0, which excludes Visual Studio 2022.
- Consume, in the extension, the version-invariant notification contract that pull request #1612 added to
  `Metalama.Framework.DesignTime.Contracts`, so that no code path of the extension references
  `Metalama.Framework.DesignTime.Rpc`. The contract is delivered, so this bullet is consumption and not design. It is
  the work that `metalama/Metalama.Vsx.Public` issue 17 describes, on the assumption that the issue is still open.
- Report the lowest version of the extension that will still be installed on a user machine when 2027.0 ships, and
  the versions of the five flowed dependencies that this version pins. That measurement is what releases the pins,
  and nothing in `metalama/Metalama` can produce it.
- Verify that the extension loads `Metalama.Framework.DesignTime.Contracts` of PB-2027.0 in Visual Studio 2026 and in
  Visual Studio 2027, on the target frameworks that project declares.
- State whether the extension still needs the Roslyn 4.0.1 override and the `Newtonsoft.Json` version of the contracts
  project once its own floor is Visual Studio 2026, so that the two pins can be re-derived.
- State whether the pipe-based delivery of `ServiceHubServerEndpoint` may be removed in 2027.0, or must be kept for a
  further release because an unmigrated extension version is still installed.

#### Acceptance criteria

- A document of `metalama/Metalama.Vsx` states which Visual Studio versions the extension supports for 2027.0, and
  that set agrees with PB-2027.0.
- No source file of the extension references `Metalama.Framework.DesignTime.Rpc`.
- The lowest installed extension version and the five dependency versions it pins are recorded, with the date of the
  measurement, so that `Directory.Packages.md:387` can be replaced by an answer instead of a condition.
- The answer states either that the five pins may be raised, or why they stay and what has to happen before they
  move.
- A design-time verification on Visual Studio 2026 and on Visual Studio 2027 shows Metalama diagnostics, code lens
  and generated code, which is the check that item 3 of the checklist of [`platform-support.md`](../../platform-support.md)
  describes and which cannot be replaced by reading a log of a successful build.

#### Not in scope

This story does not edit `Directory.Packages.props` or [`Directory.Packages.md`](../../../../Directory.Packages.md).
Raising the five pins and rewriting the forward-looking item is a separate pull request in `metalama/Metalama`, and it
is written once this story reports its measurement. It is also not part of S-11, which re-derives the pins that are
capped by the Visual Studio installation of the build machine and not the pins that are capped by a separately
deployed extension. This story does not change `Metalama.Framework.DesignTime.Contracts`, whose GUIDs are frozen
forever by the rule at `cross-process-communication.md:57`, and to which pull request #1612 has already added the
notification contract that the extension has to consume.

— Claude for @gfraiteur
