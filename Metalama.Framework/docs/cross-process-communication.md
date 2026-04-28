# Cross-Process and Cross-Version Communication

This document describes the principles governing communication between Metalama-related processes and across Metalama versions. It exists because confusing the two axes — *cross-process* vs *cross-version* — leads to subtle wire-protocol incompatibilities that surface as `ConnectionLostException` or `FileLoadException` long after the offending change merges.

## The two axes

Communication in Metalama crosses one of two boundaries (or sometimes both):

- **Cross-process**: two operating-system processes exchange data. Each process has its own CLR, its own AppDomain, its own loaded assemblies. They can only exchange *bytes* (over a named pipe, file, or similar). Examples: the analyzer process inside `devenv` talking to a Metalama-spawned helper process; the VS CodeLens host (`ServiceHub.Host.AnyCPU`) talking to the analyzer.
- **Cross-version**: code compiled against one version of a Metalama assembly runs in the same CLR as code compiled against a different version of the same assembly. Examples: the user's project references Metalama 2026.1.x while the installed VS extension (`Metalama.Vsx`) was built against Metalama 2026.0.x; both copies of `Metalama.Framework.DesignTime.Contracts.dll` get loaded into `devenv`.

The two boundaries demand different mechanisms. Mixing them is forbidden.

## The three rules

| Boundary | Mechanism | Where it lives |
|---|---|---|
| Cross-process, **same** Metalama version | RPC over named pipe (StreamJsonRpc + MessagePack framing) | `Metalama.Framework.DesignTime.Rpc` |
| **Cross-version**, same process | CLR type equivalence via `[Guid]` / `[ComImport]` / `[TypeIdentifier]` | `Metalama.Framework.DesignTime.Contracts` |
| Cross-process, cross-version | **Not allowed.** | n/a |

The third rule is the strict one: there is **no path** for a 2026.0 process to talk over a pipe to a 2026.1 process. If that scenario arises in a design discussion, the design is wrong. Pull the boundary back to one of the first two rows.

## Same-version, cross-process: `Metalama.Framework.DesignTime.Rpc`

`Metalama.Framework.DesignTime.Rpc` is the internal RPC layer used between Metalama-version-locked processes.

**Guarantees**:
- Both ends are deployed from the **same** Metalama NuGet payload (the user's project pulls one Metalama version; every helper process Metalama spawns shares it).
- Wire format may evolve freely between Metalama versions. JSON, MessagePack, and any future framing are all acceptable choices for this layer.
- The RPC contract types (interfaces extending `IRpcApi`, event-data classes deriving from `RpcEventData`) carry the `[RpcContract]` documentation marker, but **no `[Guid]`**. They are *not* cross-version-stable, and that is intentional.

**Architecture** (see also `src/Metalama.Framework.DesignTime.Rpc/CLAUDE.md`):

```
ServerEndpoint (in Metalama process A)  ←── named pipe ──→  ClientEndpoint (in Metalama process B)
        ▲                                                              ▲
        │ implements                                                   │ proxies
        │                                                              │
   RpcService<TApi>                                                RpcClient<TApi>
        ▲                                                              ▲
        └─────────── shared TApi ── e.g. IEventHubRpcApi ──────────────┘
                  (deployed from one Metalama-version NuGet)
```

**Who may consume this layer**: only code that ships *with* the Metalama NuGet payload, i.e. lives under `Metalama.Framework.DesignTime` and is loaded into the analyzer-side process. External consumers (Visual Studio extensions, third-party tools, anything not built from the same Metalama source-tree commit) **must not** reference `Metalama.Framework.DesignTime.Rpc`.

## Cross-version, same-process: `Metalama.Framework.DesignTime.Contracts`

`Metalama.Framework.DesignTime.Contracts` is the only assembly designed to be loaded in multiple physical copies (one per Metalama version) into the same CLR and have those copies behave as one logical type.

**Mechanism**: every public interface and DTO is annotated with a fixed `[Guid("…")]`. The CLR's COM type-equivalence rules treat two types as identical when they share that GUID, full type name, and method signatures, regardless of the declaring assembly's identity. So when `devenv` loads:

- `Metalama.Framework.DesignTime.Contracts.dll` from the VSX install (e.g. compiled against Metalama 2026.0.12), and
- `Metalama.Framework.DesignTime.Contracts.dll` from the user-project NuGet (e.g. compiled against Metalama 2026.1.x),

a value flowing from one to the other satisfies the type-identity check on both sides without explicit conversion. The two physical copies are *the same type* to the CLR.

**Guarantees**:
- GUIDs on every contract type are **frozen forever** once published. Renaming a type, adding a method to an interface, changing a parameter type, or reordering members **breaks** the equivalence and must be done as an additive new contract with its own GUID.
- Method signatures on `[Guid]`-marked interfaces are part of the wire contract; they are evolved only by adding new interfaces (e.g., `ICodeLensService2`), never by mutating existing ones.
- DTO classes carry a `[Guid]` and may only be extended with members whose absence on the other side is harmless (the older copy will simply not see the new field).

**Existing precedent**: see `Metalama.Framework.DesignTime.Contracts/CodeLens/ICodeLensService.cs` (`[Guid("9E3E6194-302E-4F36-8612-FD2CA0190F21")]`), `EntryPoint/IDesignTimeEntryPointManager.cs`, etc. Every interface and DTO in that folder follows the pattern.

**Who may consume this layer**: anyone — VSX, third-party extensions, the Metalama analyzer itself. This is the only public cross-version-safe surface.

## Why cross-process, cross-version is not allowed

A pipe carries bytes; the two processes never share a CLR; COM type equivalence does not apply. To make a cross-version pipe work you would need a **versioned wire protocol** — explicit versioning of every message, negotiated capabilities, framing-format negotiation, and a forward-compatibility regime on every field. Metalama does not have that, and adding it would duplicate the cross-version-safety story already provided by the Contracts layer.

The supported route for any cross-version need that *appears* to be cross-process is to push the boundary inward:

1. Add or extend a `[Guid]`-marked interface in `Metalama.Framework.DesignTime.Contracts`.
2. Have the cross-version consumer (e.g., VSX) talk *only* to that interface.
3. Behind the interface, the *same-version* Metalama implementation may use `Metalama.Framework.DesignTime.Rpc` to fan out to other Metalama processes — but that pipe carries only same-version traffic, so it is governed by Rule 1, not Rule 3.

## Putting it together: a worked example

The CodeLens notification path traverses both boundaries:

1. **Cross-version, same-process** (VSX ↔ Metalama in `devenv`):
   - VSX (any version) calls a `[Guid]`-marked interface defined in `Metalama.Framework.DesignTime.Contracts`.
   - The Metalama-side implementation lives in the Metalama-NuGet-deployed assemblies extracted into `devenv`.
   - Both copies of `Metalama.Framework.DesignTime.Contracts.dll` (VSX-bundled and Metalama-deployed) coexist in the same CLR; type equivalence makes the call resolve.
2. **Same-version, cross-process** (Metalama ↔ Metalama helper):
   - Inside the Metalama implementation, if a notification needs to cross to another Metalama process (e.g., a CodeLens helper), the call hops to `Metalama.Framework.DesignTime.Rpc` over a named pipe.
   - Both ends are the same Metalama version, so MessagePack framing and `[RpcContract]` types are safe.

VSX never calls into `Metalama.Framework.DesignTime.Rpc` directly — that would be a cross-process *and* cross-version path, which Rule 3 forbids.

## Symptoms of violating the rules

| Symptom | Likely violation |
|---|---|
| `StreamJsonRpc.ConnectionLostException` during initial RPC handshake from a Visual Studio extension | VSX is consuming `Metalama.Framework.DesignTime.Rpc` directly across versions (Rule 3). Move the call onto a `[Guid]`-marked Contracts interface. |
| `System.IO.FileLoadException: assembly's manifest definition does not match the assembly reference` for a `Metalama.Framework.DesignTime.Rpc.*` type, on a VSX call path | Same — cross-version use of the Rpc layer. |
| `InvalidCastException` on a `Metalama.Framework.DesignTime.Contracts` type | Missing or mismatched `[Guid]` on a Contracts type. The two physical copies aren't unifying. Check that every interface in the call chain carries `[Guid]` and that the GUIDs match across versions. |
| Adding a method to an existing `[Guid]`-marked interface compiles fine but breaks the older client at runtime | The interface contract is frozen by its GUID. Add a *new* interface (`IFoo2`) with its own GUID instead. |

## Checklist when adding a new cross-version contract

When you are adding a type that needs to cross a Metalama-version boundary:

- [ ] The type lives in `Metalama.Framework.DesignTime.Contracts`.
- [ ] The type has `[Guid("…")]` with a freshly minted GUID.
- [ ] If it is an interface, it has `[ComImport]` and `[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]` as needed for type-equivalence (follow the existing pattern in the folder).
- [ ] No members of the type reference a non-Contracts type that lacks `[Guid]`.
- [ ] The GUID, type name, namespace, and member signatures are documented as frozen.
- [ ] Future extensions go through *new* interfaces with their own GUIDs, never by mutating this one.

## Checklist when adding a new same-version RPC contract

When you are adding a type that only flows between Metalama-NuGet-deployed processes built from the same source tree:

- [ ] The type lives in `Metalama.Framework.DesignTime.Rpc` or under `Metalama.Framework.DesignTime/.../Rpc`.
- [ ] The type carries `[RpcContract]` (documentation marker only).
- [ ] The type **does not** have `[Guid]` (it is not cross-version-stable, and adding `[Guid]` here would mislead future readers about its scope).
- [ ] No external consumer (VSX, third-party tooling) is referencing this type.
