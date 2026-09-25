# Mechanism decision

> Part of the [call-site interceptors design](README.md). Previous: [03-background.md](03-background.md) | Next: [05a-api-registration.md](05a-api-registration.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 4. Mechanism decision

### 4.1 Options

| Option | Description |
|---|---|
| A | Emit C# interceptors: generate interceptor methods with `[InterceptsLocation]` attributes, and let the compiler substitute calls during lowering. |
| B | Rewrite call sites syntactically in the linker, as the existing `[OnInitialized]` call-site advice does. |

### 4.2 Comparison

| Criterion | Option A (C# interceptors) | Option B (linker rewrite) |
|---|---|---|
| Await targets (R2) | Not possible. C# interceptors apply only to invocations. | Supported. |
| Local-function placements (R12) | Not possible. CS9146 requires an ordinary method (RC `Symbols\Source\SourceMethodSymbolWithAttributes.cs:1364-1387`). | Supported. |
| Instance interceptors that use the caller's `this` (R13) | Not possible. The interceptor receives the receiver, not the calling object (RC `Lowering\LocalRewriter\LocalRewriter_Call.cs:213-270`). | Supported. |
| Interceptors in generic calling types | Not possible. CS9138. | Supported. |
| Implicit conversions in the signature (R9) | Not possible. Types must be identical (CS9144). | Supported. |
| `base.M()` calls | A static interceptor cannot make a non-virtual call. | Supported with an instance interceptor in the calling type. |
| Interceptor in the same file as the call | The attribute would contain the hash of its own file. | No constraint. |
| Location data | Computed on the final text after linking, for each copy of a call site produced by inlining. | Node identity: the source node is the key of the linker input, and the injection rewriter rewrites the node before any copy exists. |
| Conflicts with source generators that run after Metalama | CS9153 whenever a generator intercepts the same call. Metalama cannot prevent it. | The rewritten call site calls the Metalama interceptor. The original call inside the interceptor remains interceptable by a generator. |
| Readability of transformed code and test baselines | Opaque base64 data that changes with the source. | Readable calls. |
| Argument semantics (evaluation order, named and optional arguments, `params`, caller information, struct receivers, conditional access) | Free: the compiler binds the original call. | Must be reproduced explicitly. Section [6](06a-call-site-model.md#6-call-site-semantics-and-signature-derivation-for-invocations) is the checklist. |
| Precedent | None in Metalama. | PostSharp call-site weaving; the `[OnInitialized]` call-site advice. |

### 4.3 Decision

Option B (baseline B1). The requirements R2 (await), R9 (implicit conversions), R12 (local functions) and R13 (caller's `this`) cannot be met with option A. The cost of option B is the explicit reproduction of argument semantics, which section [6](06a-call-site-model.md#6-call-site-semantics-and-signature-derivation-for-invocations) specifies call-site shape by call-site shape, and which the runtime tests and the equivalence corpus of section [12](12-test-plan.md#12-test-plan) verify.

A bridge to C# interceptors stays possible later for shapes that a syntactic rewrite cannot express, because the reference index records the same simple-name token that `GetInterceptableLocation` uses (ENG26 `ReferenceGraph\ReferenceIndexWalker.cs:103`).
