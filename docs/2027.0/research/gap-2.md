# Gap 2 — Reference assemblies and the metadata emission / reading of the new .NET 11 attributes and flags

Research date: **2026-09-03**. .NET 11 / C# 15 GA: **November 2026**.

All statements below were verified against the **`main`** branch of `dotnet/roslyn`,
`dotnet/runtime`, `dotnet/csharplang` and `dotnet/sdk` as of 2026-09-03, plus the
GitHub code-search and commit-history APIs. No blog aggregators were used.

Roslyn version on `main` (`eng/Versions.props`): **MajorVersion 5, MinorVersion 12, PatchVersion 0**,
i.e. the compiler that ships with the .NET 11 SDK is the **Roslyn 5.12** line.
Source: <https://github.com/dotnet/roslyn/blob/main/eng/Versions.props>

---

## 0. Executive summary (the six questions, answered)

| # | Question | Answer |
|---|---|---|
| 1 | Ref-assembly keep/drop rules, and any change in Roslyn 5.0–5.12 | Rules are unchanged since C# 7.1 (last substantive edit to `docs/features/refout.md` was 2021-06-17; last edit of any kind 2024-02-21, a link fix). The single filter is `Microsoft.Cci.Extensions.ShouldInclude(ITypeDefinitionMember, EmitContext)` in `src/Compilers/Core/Portable/PEWriter/Members.cs`. **No change in the .NET 11 wave.** |
| 2 | Which new artifacts survive into a ref assembly | **All of them survive**, with one exception and one caveat — see §3. `MethodImplAttributes.Async` survives verbatim on the MethodDef row. `IsClosedTypeAttribute` including `DerivedTypes` survives. `[CompilerFeatureRequired("ClosedClasses")]` survives iff the constructor itself survives. `UnionAttribute`, `IUnion`, the union constructors and `Value` survive (all `public`). `RequiresUnsafeAttribute` and module-level `MemorySafetyRulesAttribute` survive. `ExtendedLayoutAttribute` + `TypeAttributes.ExtendedLayout` survive. Every extension-lowering artifact survives, including the grouping type, the marker type, the marker method (unconditionally, **even when it would fail the visibility filter**), the skeleton `Item` property and the static `get_Item`/`set_Item` implementation methods. |
| 3 | Does a closed class read from a ref assembly give a different `GetClosedDerivedTypeInfo`? | **No.** Roslyn does not read `IsClosedTypeAttribute.DerivedTypes` at all; `PENamedTypeSymbol.CandidateClosedSubtypeDefinitions` scans every `TypeDefinition` row of the module, and ref assemblies keep **all** types. `IsComplete == false` has **nothing to do with internal derived types**: it means *at least one candidate derived type unified against the closed type but is not "speakable"* — it introduces a type parameter that does not appear on the closed type. See §5. |
| 4 | Exact values / ECMA status / named enum members | `MethodImplAttributes.Async = 0x2000` (8192): named member **exists** in .NET 11 `System.Reflection.MethodImplAttributes` and `System.Runtime.CompilerServices.MethodImplOptions`; ECMA status is a **draft addendum** (`docs/design/specs/runtime-async.md`), *not* merged into `Ecma-335-Augments.md`. `TypeAttributes.ExtendedLayout = 0x00000018` (24): named member **exists**; **already merged into `Ecma-335-Augments.md`** ("Extended Layout" section). `System.Reflection.Metadata` defines **no** named members for either — it reuses `System.Reflection.TypeAttributes`/`MethodImplAttributes` from the targeted `System.Runtime`. |
| 5 | Other `System.Reflection.Metadata` changes in .NET 11 | **No public API change.** Last commit to `src/libraries/System.Reflection.Metadata/ref/System.Reflection.Metadata.cs` is **2025-07-01** (PR #116839, a .NET 10 change). 26 commits to the library since 2025-11-01, all bug fixes / refactors — four of them are behaviourally relevant to IL writers. See §8. |
| 6 | Does ILVerify accept a `ret` that disagrees with the signature when `Async` is set? | **Yes.** `dotnet/runtime` PR **#121503** "Update ILVerify to honor the *async* flag", merged **2025-11-13**, changed `ILImporter.ImportReturn` to unwrap `Task`/`ValueTask`/`Task<T>`/`ValueTask<T>` when `_method.IsAsync`. `EcmaMethod.IsAsync` reads `MethodImplAttributes.Async` (`MethodFlags.Async = 0x02000`). Note ILVerify is **not** in the SDK; it ships as the `dotnet-ilverify` global tool. |

---

## 1. The reference-assembly contract (unchanged rules)

Primary source: <https://github.com/dotnet/roslyn/blob/main/docs/features/refout.md>

### 1.1 The four scenarios and the two flags

| Scenario | `EmitMetadataOnly` | `IncludePrivateMembers` | Command line |
|---|---|---|---|
| Regular compilation | false | true | `/out` |
| IDE metadata-only (cross-language project reference) | true | **true** | `Emit(..., EmitOptions.EmitMetadataOnly)` |
| "CoreFX" reference-only | true | **false** | `/refonly` |
| MSBuild secondary output | false | **false** | `/refout:<path>` (+ `metadataPeStream`) |

`EmitContext.IsRefAssembly => MetadataOnly && !IncludePrivateMembers`
(`src/Compilers/Core/Portable/Emit/Context.cs`).
For `/refout` the *primary* stream is written with `metadataOnly: false` and the *secondary*
`metadataPeStream` with a second `MetadataWriter` whose `metadataOnly` is `true`, so from the
point of view of everything below, `/refout`'s ref output behaves like `/refonly`.

MSBuild properties (SDK): `ProduceReferenceAssembly`, `ProduceOnlyReferenceAssembly`,
`CompileUsingReferenceAssemblies` (escape hatch, only ever checked against `false`).
`ProduceReferenceAssembly` is **defaulted to `true` by the .NET SDK** for
`.NETCoreApp >= 5.0` C#/VB projects (>= 7.0 for `.fsproj`), in
`src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.TargetFrameworkInference.targets`, lines 291–294.
So **every ordinary .NET project already goes down the ref-assembly path**.

### 1.2 What is kept

Verbatim from `refout.md`:

- "Metadata-only assembly have their method bodies replaced with a single `throw null` body,
  but include all members except anonymous types."
- "But **all types (including private or nested types) are kept** in ref assemblies.
  **All attributes are kept (even internal ones)**, as well as their (internal) constructors."
- "All virtual methods are kept. Explicit interface implementations are kept.
  Explicitly-implemented properties and events are kept, as their accessors are virtual."
- "All fields of a struct are kept."
- An assembly-level `System.Runtime.CompilerServices.ReferenceAssemblyAttribute` is added.

### 1.3 What is dropped

- Private function members (methods, properties, events).
- Internal function members, **unless** the assembly has any `InternalsVisibleTo`.
- Anonymous types.
- Manifest resources (`CommonPEModuleBuilder.GetResources`, guarded by `context.IsRefAssembly`).
- References only needed by implementation details.

### 1.4 The exact filter

`src/Compilers/Core/Portable/PEWriter/Members.cs`, `internal static class Extensions`:

```csharp
/// <summary>
/// When emitting ref assemblies, some members will not be included.
/// </summary>
public static bool ShouldInclude(this ITypeDefinitionMember member, EmitContext context)
{
    if (context.IncludePrivateMembers) return true;
    var method = member as IMethodDefinition;
    if (method != null && method.IsVirtual) return true;

    bool acceptBasedOnVisibility = true;
    switch (member.Visibility)
    {
        case TypeMemberVisibility.Private:
            acceptBasedOnVisibility = context.IncludePrivateMembers; break;
        case TypeMemberVisibility.Assembly:
        case TypeMemberVisibility.FamilyAndAssembly:
            acceptBasedOnVisibility = context.IncludePrivateMembers
                || context.Module.SourceAssemblyOpt?.InternalsAreVisible == true;
            break;
    }
    if (acceptBasedOnVisibility) return true;

    if (method?.IsStatic == true)              // static explicit interface impls
        foreach (var mi in method.ContainingTypeDefinition.GetExplicitImplementationOverrides(context))
            if (mi.ImplementingMethod == method) return true;

    if (method != null && (context.Module.PEEntryPoint == method
                        || context.Module.DebugEntryPoint == method)) return true;

    return false;
}
```

Note there is **no type-level filter**: `ShouldInclude` is only ever applied to
`ITypeDefinitionMember`s that are methods, properties, events and fields, never to
`INestedTypeDefinition`s (with the caveat that a nested type *is* an `ITypeDefinitionMember`;
Roslyn simply never calls `ShouldInclude` on one — see
`NamedTypeSymbolAdapter.GetNestedTypes`, which yields unconditionally).

Constructors get an extra rule in
`src/Compilers/CSharp/Portable/Emitter/Model/NamedTypeSymbolAdapter.cs` (~line 627):

```csharp
// Don't compute IsAttributeType if IncludePrivateMembers is true, as we'll include it anyway.
bool alwaysIncludeConstructors = context.IncludePrivateMembers
    || AdaptedNamedTypeSymbol.DeclaringCompilation.IsAttributeType(AdaptedNamedTypeSymbol);
```

so **all** constructors of an attribute type survive even in a ref assembly, but a
private constructor of a non-attribute type does not.

Struct fields (~line 436): `if (isStruct || f.GetCciAdapter().ShouldInclude(context))`.

### 1.5 The `throw null` body

`src/Compilers/Core/Portable/PEWriter/MetadataWriter.cs`:

```csharp
private const byte TinyFormat = 2;
private const int ThrowNullCodeSize = 2;
private static readonly ImmutableArray<byte> ThrowNullEncodedBody =
    ImmutableArray.Create(
        (byte)((ThrowNullCodeSize << 2) | TinyFormat),
        (byte)ILOpCode.Ldnull,
        (byte)ILOpCode.Throw);
```

`SerializeThrowNullMethodBodies` (line ~2956) emits **one single shared body blob**
for the whole module and points every `MethodDef` whose `HasBody` is true at it;
methods with `HasBody == false` get `bodyOffset = -1` (nil RVA).
`HasBody` is `Cci.DefaultImplementations.HasBody(this)` — false for abstract and
`IsExternal` methods.

**Consequence for runtime-async:** a ref-assembly body is `ldnull; throw` — it contains
**no `ret` instruction at all**. The runtime-async return convention (empty stack for
`Task`/`ValueTask`, `T` on the stack for `Task<T>`/`ValueTask<T>`) is therefore never
exercised on the ref-assembly path. There is no possible disagreement to detect.

### 1.6 Where MetadataOnly actually changes emission

Grepping `MetadataOnly` / `IsRefAssembly` across the emitter:

- `MetadataWriter.SerializeMethodBodies` → `SerializeThrowNullMethodBodies` (line 1837).
- `PEModuleBuilder.GetAnonymousTypeDefinitions` returns an empty enumerable when
  `context.MetadataOnly` (`src/Compilers/CSharp/Portable/Emitter/Model/PEModuleBuilder.cs`, line 546).
- `CommonPEModuleBuilder.GetResources` returns empty when `context.IsRefAssembly` (line 587).
- `MetadataWriter.PopulateCustomAttributeTableRows` passes `Context.IsRefAssembly` into
  `module.GetSourceAssemblyAttributes(...)` (line 2060) — this is where
  `ReferenceAssemblyAttribute` is added.
- `MethodCompiler` skips body compilation entirely and even filters the
  "method must have a body" declaration diagnostic.

That is the complete list. **Custom attributes, `MethodImplAttributes`, `TypeAttributes`,
interface implementations, generic constraints and `MethodSemantics` rows all go through
the identical code path for ref and implementation assemblies.**

Specifically, `MetadataWriter.PopulateMethodTableRows` (line ~2661):

```csharp
metadata.AddMethodDefinition(
    attributes:     GetMethodAttributes(methodDef),
    implAttributes: methodDef.GetImplementationAttributes(Context),   // <-- unconditional
    name:           GetStringHandleForNameAndCheckLength(methodDef.Name, methodDef),
    signature:      GetMethodSignatureHandle(methodDef),
    bodyOffset:     methodBodyOffsets[i],
    parameterList:  GetFirstParameterHandle(methodDef));
```

and `PopulateCustomAttributeTableRows` guards only on `IsFullMetadata`
(full versus **EnC delta** metadata, *not* metadata-only), so module- and
assembly-level attributes are emitted for ref assemblies too.

---

## 2. `MethodImplAttributes.Async` (0x2000) in detail

### 2.1 Value and definitions

| Where | Declaration |
|---|---|
| `dotnet/runtime` `src/libraries/System.Private.CoreLib/src/System/Reflection/MethodImplAttributes.cs` | `Async = 0x2000,` (in a **non-`[Flags]`** enum, "This Enum matches the CorMethodImpl defined in CorHdr.h") |
| `dotnet/runtime` `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/MethodImplOptions.cs` | `Async = 0x2000` (in a `[Flags]` enum) |
| `dotnet/runtime` `src/libraries/System.Runtime/ref/System.Runtime.cs` line 13681 | `Async = 8192,` in `public enum MethodImplAttributes` |
| `dotnet/runtime` `src/libraries/System.Runtime/ref/System.Runtime.cs` line 15014 | `Async = 8192,` in `public enum MethodImplOptions` |
| `dotnet/runtime` `src/coreclr/inc/corhdr.h` | `CorMethodImpl` value |

So **named members exist in .NET 11's `System.Runtime` reference assembly**. They do **not**
exist in netstandard2.0 / .NET 8 / .NET 9 / .NET 10 `System.Runtime`.
Roslyn itself works around this with a C# 14 extension-member shim,
`src/Compilers/Core/Portable/MethodImplExtensions.cs`:

```csharp
internal static class MethodImplAttributeExtensions
{
    extension(MethodImplAttributes)
    {
        public static MethodImplAttributes Async
        {
            get
            {
#if NET10_0_OR_GREATER
                Debug.Assert(MethodImplAttributes.Async == (MethodImplAttributes)0x2000);
#endif
                return (MethodImplAttributes)0x2000;
            }
        }
    }
}
```

(There is an identical `MethodImplOptionsExtensions`. Note the `#if NET10_0_OR_GREATER`
guard — the named member is present in the runtime Roslyn builds against.)

### 2.2 ECMA-335 status

The change lives in a **separate draft document**, not in the augments file:

- `dotnet/runtime` `docs/design/specs/runtime-async.md`, first line:
  "This document is a draft of changes to ECMA-335 for the 'runtime async' feature.
  When the feature is officially supported, it can be merged into the final ECMA-335 augments document."
- Section "II.23.1.11 Flags for methods [MethodImplAttributes]" adds the row
  `| Async | 0x2000 | Method is an Async Method. |` and states
  "The flag is represented in IL by the `async` keyword. Tools like `ilasm` and `ildasm` recognize this flag."
- `docs/design/specs/Ecma-335-Augments.md` contains **no** async section (grep for `Async`
  / `0x2000` in that file returns nothing). Its table of contents ends with
  "Extended layout" and "Implicit argument coercion rules".

Normative rules from the draft (I.8.4.5):

- The flag only has effect on method definitions returning `Task`, `ValueTask`, `Task<T>` or `ValueTask<T>`.
- The flag only has effect on method definitions with a **CIL implementation**.
- Async method definitions are only valid inside "async-capable assemblies": one that
  references a corlib whose `abstract sealed class RuntimeFeature` has a
  `public const string` field named `Async`.
- `Async` + `Synchronized` is invalid. `byref`/ref-like returns invalid. `vararg` invalid.
- The `ret` convention: "For async methods, the stack should be empty in the case of
  `Task` or `ValueTask`, or the type argument in the case of `Task<T>` or `ValueTask<T>`."
- Temporary restrictions: `tail.` prefix forbidden, `localloc` forbidden.
- Permanent restrictions: by-ref locals not hoisted across suspension points; suspension
  points not allowed in `catch`/`filter`/`finally`/`fault` (allowed in the protected `try`).

### 2.3 How Roslyn sets the bit

`src/Compilers/CSharp/Portable/Symbols/Source/SourceMethodSymbolWithAttributes.cs`, ~line 1750:

```csharp
internal override System.Reflection.MethodImplAttributes ImplementationAttributes
{
    get
    {
        var data = GetDecodedWellKnownAttributeData();
        var result = (data != null) ? data.MethodImplAttributes : default(System.Reflection.MethodImplAttributes);
        // ... (Runtime | InternalCall for certain synthesized members)
        AddAsyncImplAttributeIfNeeded(ref result);
        return result;
    }
}

protected void AddAsyncImplAttributeIfNeeded(ref System.Reflection.MethodImplAttributes result)
{
    if (this.IsAsync && this.DeclaringCompilation.IsRuntimeAsyncEnabledIn(this))
    {
        // When a method is emitted using runtime async, we add MethodImplAttributes.Async to indicate to the
        // ...
        result |= System.Reflection.MethodImplAttributes.Async;
    }
}
```

`SynthesizedSimpleProgramEntryPointSymbol` calls the same helper.

`CSharpCompilation.IsRuntimeAsyncEnabledIn(Symbol?)`
(`src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs`, ~line 351):

1. `Assembly.RuntimeSupportsAsyncMethods` must be true (corlib exposes `RuntimeFeature.Async`).
2. The symbol must be a `MethodSymbol { IsAsync: true }`.
3. Per-method `[RuntimeAsyncMethodGeneration(bool)]` wins
   (`MethodWellKnownAttributeData.RuntimeAsyncMethodGenerationSetting`, a `ThreeState`);
   otherwise the compilation-wide feature flag `Feature(CodeAnalysis.Feature.RuntimeAsync) == "on"`
   decides (i.e. `/features:runtime-async=on`).
4. The (original definition of the) return type must be one of
   `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` (checked via `InternalSpecialType`).

`System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(bool)` —
`AttributeDescription.RuntimeAsyncMethodGenerationAttribute`, signature
`s_signatures_HasThis_Void_Boolean_Only`.

**Status at GA:** `docs/Language Feature Status.md` lists
`Runtime Async | main | Main feature merged into main in preview` and a separate
in-progress `Runtime Async Streams` branch. So runtime-async is **opt-in / preview**
at .NET 11 GA. `MethodImplAttributes.Async` will not appear in ordinary builds unless
`/features:runtime-async=on` or the per-method attribute is used.

### 2.4 Does the bit survive `/refout` / `/refonly`?

**Yes, verbatim.** `ImplementationAttributes` is a property of the *symbol*, computed
without reference to the emit mode; `MethodSymbolAdapter.GetImplementationAttributes`
returns it; `MetadataWriter.PopulateMethodTableRows` writes it into the MethodDef row
unconditionally. The resulting ref assembly therefore contains a MethodDef whose
`ImplAttributes` is `IL | Managed | Async` (0x2000) and whose body is `ldnull; throw`.

**The one exception** — `src/Compilers/CSharp/Portable/Emitter/Model/MethodSymbolAdapter.cs`, line 458:

```csharp
System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
{
    CheckDefinitionInvariant();
    return AdaptedMethodSymbol.ContainingType.IsExtension ? default : AdaptedMethodSymbol.ImplementationAttributes;
}
```

Members whose containing type is an **extension block** get `implAttributes = 0`.
So an `async` extension method loses the `Async` bit on its *skeleton* member inside the
grouping type, but keeps it on the *implementation method* emitted on the enclosing static
class. This is true in implementation and reference assemblies alike.

### 2.5 Reading the bit back

- **Roslyn** — `PEMethodSymbol` stores `private readonly ushort _implFlags;` and exposes
  `internal override MethodImplAttributes ImplementationAttributes => (MethodImplAttributes)_implFlags;`
  (`src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEMethodSymbol.cs`, lines 402, 486).
  This is surfaced publicly as **`IMethodSymbol.MethodImplementationFlags`**
  (`src/Compilers/Core/Portable/Symbols/IMethodSymbol.cs`, line 282,
  "Returns the implementation flags for the given method symbol."), typed
  `System.Reflection.MethodImplAttributes`. So a Roslyn-based tool can test
  `(symbol.MethodImplementationFlags & (MethodImplAttributes)0x2000) != 0`.
  Roslyn does **not** map the bit onto `IMethodSymbol.IsAsync` for metadata methods —
  code search for `MethodImplAttributes.Async` in `dotnet/roslyn` returns exactly two files
  (`MethodImplExtensions.cs`, `SourceMethodSymbolWithAttributes.cs`), both on the emit side.
- **System.Reflection.Metadata** — `MethodDefinition.ImplAttributes` is typed
  `System.Reflection.MethodImplAttributes`; the raw `ushort` is returned unchanged.
  SRM does **not** define its own copy of the enum, so whether `Async` is a *named* member
  depends purely on which `System.Runtime` the consuming code compiles against.
  SRM reads and writes the bit fine on any target.
- **Mono.Cecil** (`jbevain/cecil`, branch `master`) — `Mono.Cecil/MethodImplAttributes.cs`
  is Cecil's **own** `[Flags] public enum MethodImplAttributes : ushort` and has **no
  `Async` member** (highest value declared is `AggressiveOptimization = 0x0200`).
  However `AssemblyReader.cs` line 1774 does
  `method.ImplAttributes = (MethodImplAttributes) ReadUInt16 ();` and the writer writes the
  raw `ushort` back, so unknown bits **round-trip losslessly**; they are just unnamed.
- **CoreCLR type system / ILVerify** — `src/coreclr/tools/Common/TypeSystem/Ecma/EcmaMethod.cs`
  declares `public const int Async = 0x02000;` in its private `MethodFlags`, sets it from
  `(methodImplAttributes & MethodImplAttributes.Async) != 0` and exposes `EcmaMethod.IsAsync`.
- **ilasm / ildasm** — updated to recognize the `async` keyword in `dotnet/runtime` PR
  **#115658**; the managed ilasm (`src/tools/ilasm/src/ILAssembler`) has `async` in its
  ANTLR grammar (`gen/CIL.g4`, `gen/CIL.tokens`, `GrammarVisitor.cs`).

---

## 3. Per-artifact ref-assembly survival table

Legend: **Survives** = present in the `/refonly` (and `/refout`) output with the stated fidelity.

### 3.1 `MethodImplAttributes.Async` (0x2000) on a runtime-async method

**Survives, full fidelity.** The MethodDef row's `ImplAttributes` field is written by the
same call for both emit modes (§1.6, §2.4). The paradox the assignment names is real but
harmless: the flag describes a *body convention* in an assembly whose bodies are all
`ldnull; throw`. Since `throw` bodies contain no `ret`, no verifier or reader can observe
the contradiction. Exception: skeleton members inside an extension block get
`implAttributes = 0` (§2.4).

### 3.2 `System.Runtime.CompilerServices.IsClosedTypeAttribute`, including `DerivedTypes`

**Survives, full fidelity, including the named argument.** Ref assemblies keep all
attributes and all types.

Runtime definition — `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IsClosedTypeAttribute.cs`:

```csharp
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IsClosedTypeAttribute : Attribute
{
    private Type[] _derivedTypes = Type.EmptyTypes;
    public IsClosedTypeAttribute() { }
    /// <summary>Gets or sets the derived types of the closed type.</summary>
    /// <value>An array of the derived types of the closed type. A null value is normalized to an empty array.</value>
    public Type[] DerivedTypes { get => _derivedTypes; set => _derivedTypes = value ?? Type.EmptyTypes; }
}
```

Ref-assembly declaration (`src/libraries/System.Runtime/ref/System.Runtime.cs`, ~line 14929):

```csharp
[System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=false)]
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public sealed partial class IsClosedTypeAttribute : System.Attribute
{
    public IsClosedTypeAttribute() { }
    public System.Type[] DerivedTypes { get { throw null; } set { } }
}
```

Emission — `src/Compilers/CSharp/Portable/Symbols/Source/SourceNamedTypeSymbol.cs`, ~line 1805:

```csharp
if (IsClosed)
{
    ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>> namedArguments;
    var derivedTypesProperty = (PropertySymbol)compilation.GetWellKnownTypeMember(
        WellKnownMember.System_Runtime_CompilerServices_IsClosedTypeAttribute__DerivedTypes);
    if (derivedTypesProperty is not null)
    {
        var propertyType = (ArrayTypeSymbol)derivedTypesProperty.Type;
        var derivedTypesConstant = new TypedConstant(
            propertyType,
            CandidateClosedSubtypeDefinitions.SelectAsArray(
                static (subtype, elementType) => new TypedConstant(elementType, TypedConstantKind.Type,
                    subtype.GetUnboundGenericTypeOrSelf()), propertyType.ElementType));
        namedArguments = [new KeyValuePair<WellKnownMember, TypedConstant>(
            WellKnownMember.System_Runtime_CompilerServices_IsClosedTypeAttribute__DerivedTypes,
            derivedTypesConstant)];
    }
    else
    {
        namedArguments = default;   // older corlib without the property: parameterless ctor only
    }

    AddSynthesizedAttribute(ref attributes,
        compilation.TrySynthesizeAttribute(
            WellKnownMember.System_Runtime_CompilerServices_IsClosedTypeAttribute__ctor,
            namedArguments: namedArguments));
}
```

Key facts:

- The array is built from **`CandidateClosedSubtypeDefinitions`**, i.e. *all* direct
  derived types declared in the same module, **regardless of accessibility**. There is
  **no filtering of `internal` (or `private`) derived types**. The premise in the
  assignment brief ("`DerivedTypes` may omit internal derived types") is **false** for
  the current implementation.
- Generic derived types are recorded as their **unbound generic definition**
  (`subtype.GetUnboundGenericTypeOrSelf()`, i.e. `typeof(D<>)`).
- `IsComplete == false` is **not** related to `DerivedTypes` at all (§5).
- `AttributeDescription.IsClosedTypeAttribute` declares only
  `s_signatures_HasThis_Void_Only` — the parameterless constructor. Roslyn's *reader*
  never decodes the named argument.
- The C# language proposal (`dotnet/csharplang` `proposals/csharp-15.0/closed-hierarchies.md`,
  "Lowering") still shows the attribute as `public sealed class IsClosedTypeAttribute : Attribute { }`
  with no `DerivedTypes`; the property was added later, on the runtime + Roslyn side,
  for reflection consumers.
- The one confirmed consumer of `DerivedTypes` is **System.Text.Json**:
  `src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/DefaultJsonTypeInfoResolver.Helpers.cs`,
  methods `IsClosedType(Type, out Type[]?)`, `GetDeclaredDerivedTypes(CustomAttributeData)`
  and `GetClosedDerivedTypes(...)`, reached from `PopulatePolymorphismMetadata` and driven by
  `JsonPolymorphicAttribute.InferClosedTypePolymorphism` / `JsonSerializerOptions.InferClosedTypePolymorphism`.
  STJ matches the attribute **by full name** (`"System.Runtime.CompilerServices.IsClosedTypeAttribute"`),
  explicitly because "the C# compiler polyfills the attribute directly into the consuming
  assembly" when targeting a runtime that predates it. It also handles the CoreCLR /
  Mono difference in how array-valued named arguments materialize
  (`Type[]` on Mono, `IList<CustomAttributeTypedArgument>` on CoreCLR).

### 3.3 `[CompilerFeatureRequired("ClosedClasses")]` on constructors

**Survives whenever the constructor itself survives.**

- `Microsoft.CodeAnalysis.CompilerFeatureRequiredFeatures` (`src/Compilers/Core/Portable/Symbols/CompilerFeatureRequiredFeatures.cs`):
  `None = 0, RefStructs = 1 << 0, RequiredMembers = 1 << 1, UserDefinedCompoundAssignmentOperators = 1 << 2, ClosedClasses = 1 << 3`.
  The metadata string is `nameof(CompilerFeatureRequiredFeatures.ClosedClasses)` = `"ClosedClasses"`.
- Emission — `src/Compilers/CSharp/Portable/Symbols/MethodSymbol.cs`, line 1309:

```csharp
protected static void AddClosedClassesFeatureRequiredAttribute(
    ref ArrayBuilder<CSharpAttributeData> attributes, MethodSymbol methodToAttribute)
{
    if (methodToAttribute.ContainingType.IsClosed)
    {
        CSharpCompilation declaringCompilation = methodToAttribute.DeclaringCompilation;
        AddSynthesizedAttribute(ref attributes,
            declaringCompilation.TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor,
                [new TypedConstant(declaringCompilation.GetSpecialType(SpecialType.System_String),
                                   TypedConstantKind.Primitive,
                                   nameof(CompilerFeatureRequiredFeatures.ClosedClasses))]));
    }
}
```

  called from `SourceMethodSymbol.AddSynthesizedAttributes` for `SourceConstructorSymbolBase`
  (line 249–253) and from `SynthesizedInstanceConstructor.AddSynthesizedAttributes` (line 326).
- Ref-assembly caveat: a **private** constructor of a non-attribute type is dropped by
  `ShouldInclude`, taking the attribute with it. That is benign: an external consumer
  cannot call a dropped constructor anyway, and the derived types that *can* call it are
  in the same module. Public / protected constructors of closed classes survive with the
  attribute intact, which is the case that matters for blocking down-level derivation.
- The proposal (`closed-hierarchies.md`) explicitly notes: "unlike for the 'required members'
  feature, an `ObsoleteAttribute` is **not** emitted in addition to the
  `CompilerFeatureRequiredAttribute`. Only the latter is emitted."
- Reading side: `PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(...)` →
  `PEModule.GetFirstUnsupportedCompilerFeatureFromToken(handle, decoder, allowedFeatures)`
  → `ERR_UnsupportedCompilerFeature` ("'{0}' requires compiler feature '{1}', which is not
  supported by this version of the C# compiler.").

### 3.4 `UnionAttribute`, `IUnion`, synthesized constructors and `Value`

**All survive.** Everything the union lowering synthesizes is `public`.

Runtime definitions:

- `System.Runtime.CompilerServices.UnionAttribute`
  (`src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/UnionAttribute.cs`):
  `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)] public sealed class UnionAttribute : Attribute { }`
- `System.Runtime.CompilerServices.IUnion`
  (`.../IUnion.cs`): `public interface IUnion { object? Value { get; } }`.
  Ref-assembly declaration at `System.Runtime.cs` ~line 14970.

Lowering (`dotnet/csharplang` `proposals/csharp-15.0/unions.md`, "Lowering"):

> A union declaration is lowered to a struct declaration with the same attributes,
> modifiers, name, type parameters and constraints, **implicit implementations of `IUnion`**,
> a **`public object? Value { get; }` auto-property**, a **public constructor for each of the
> case types**, and any members in the union declaration's body.

```csharp
public union Pet(Cat, Dog){ ... }
// lowers to
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;
    public object? Value { get; }
    ... // original body
}
```

Ref-assembly analysis:

- `UnionAttribute` on the type: kept (all attributes kept). Emitted from
  `SourceNamedTypeSymbol.AddSynthesizedAttributes` → `ShouldApplyUnionAttribute()` →
  `WellKnownMember.System_Runtime_CompilerServices_UnionAttribute__ctor`.
- `IUnion` interface implementation: `InterfaceImpl` rows are emitted unconditionally.
- Public constructors: `ShouldInclude` returns true on visibility.
- `public object? Value { get; }`: property row plus a public `get_Value` accessor — kept.
  Its compiler-generated **backing field is private**, but the lowered type is a **struct**,
  and `NamedTypeSymbolAdapter` keeps all fields of a struct (`if (isStruct || f...ShouldInclude(context))`).
  A `union class` (the proposal permits `[Union]` on classes for hand-written unions) would
  lose a private backing field in the ref assembly, which is harmless.
- Roslyn public API for consumers: `ITypeSymbol.IsUnion` ("True if language treats the type
  as a Union") and `ITypeSymbol.UnionCaseTypes` (`ImmutableArray<ITypeSymbol>`, "returns the
  case types of the union. Otherwise, returns an empty array") —
  `src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs`, lines 147–161.
  These are computed from the *shape* of the type (union constructors + `Value` property),
  so they work identically for a ref assembly, whose public shape is unchanged.

### 3.5 `System.Diagnostics.CodeAnalysis.RequiresUnsafeAttribute` on members

**Survives whenever the member survives.**

Runtime definition (`src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/RequiresUnsafeAttribute.cs`):

```csharp
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property,
                Inherited = false, AllowMultiple = false)]
#if SYSTEM_PRIVATE_CORELIB
public
#else
internal
#endif
    sealed class RequiresUnsafeAttribute : Attribute
{
    public RequiresUnsafeAttribute() { }
}
```

Public in `System.Runtime` ref (`System.Runtime.cs` ~line 9184). Note the attribute targets
do **not** include `Field`, even though the unsafe-evolution design lets fields be marked
`unsafe`; the field case is expressed differently (see the proposal's "In a type with
`[StructLayout(LayoutKind.Explicit)]` or `[ExtendedLayout]`, all instance fields must be
marked either `safe` or `unsafe`").

Roslyn:

- `AttributeDescription.RequiresUnsafeAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "RequiresUnsafeAttribute", s_signatures_HasThis_Void_Only)`
  (`AttributeDescription.cs` line 488).
- Synthesized via `PEModuleBuilder.TrySynthesizeRequiresUnsafeAttribute()` →
  `WellKnownMember.System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute__ctor` (line 1906).
- **Can be embedded** when the corlib lacks it: `EmbeddableAttributes` /
  `PEAssemblyBuilder` synthesize an internal `[Microsoft.CodeAnalysis.Embedded]` copy.
  Embedded attribute types are **kept in ref assemblies** ("all types … are kept.
  All attributes are kept (even internal ones), as well as their (internal) constructors").
- Read back by `PEMethodSymbol`, `PEPropertySymbol`, `PEEventSymbol`, `PEFieldSymbol`
  — each has a `hasRequiresUnsafeAttribute` out-parameter in its attribute-loading loop and
  **filters the attribute out of `GetAttributes()`** (it is surfaced as a symbol property,
  not as an attribute).

Ref-assembly caveat inherited from the design, and the reason the brief flags it:
`unsafe-evolution.md` §`extern` states, verbatim:

> `extern` methods from assemblies using the legacy memory safety rules are not considered
> implicitly `unsafe` because `extern` is considered implementation detail that is not part
> of public surface. **`extern` is not guaranteed to be preserved in reference assemblies.**

That is a statement about the `MethodAttributes.PInvokeImpl` / `extern` *modifier*, not about
`RequiresUnsafeAttribute`. Because a `[RequiresUnsafe]` attribute is synthesized onto the
member and attributes are always kept, the *unsafe-ness* information does survive the ref
assembly even when `extern`-ness does not. In practice: the compiler requires `extern`
members to be explicitly `safe` or `unsafe` under the new rules
(LDM 2026-04-01, 2026-04-06, 2026-04-13, 2026-05-13), so the attribute is present.

### 3.6 `System.Runtime.CompilerServices.MemorySafetyRulesAttribute` on the module

**Survives.** It is a **module-level** custom attribute, and
`MetadataWriter.AddModuleAttributesToTable` is guarded only by `IsFullMetadata`
(full vs. EnC-delta), never by `MetadataOnly`.

Runtime definition:

```csharp
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Module, Inherited = false, AllowMultiple = false)]
public sealed class MemorySafetyRulesAttribute : Attribute
{
    public MemorySafetyRulesAttribute(int version) => Version = version;
    public int Version { get; }
}
```

Emission — `src/Compilers/CSharp/Portable/Symbols/Source/SourceModuleSymbol.cs`,
`AddSynthesizedAttributes` (line 663):

```csharp
if (RequiresRefSafetyRulesAttribute())
{
    var version = ImmutableArray.Create(new TypedConstant(compilation.GetSpecialType(SpecialType.System_Int32),
        TypedConstantKind.Primitive, 11));
    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeRefSafetyRulesAttribute(version));
}

if (MemorySafetyRulesVersion != MemorySafetyRulesVersion.Version1)
{
    var version = ImmutableArray.Create(new TypedConstant(compilation.GetSpecialType(SpecialType.System_Int32),
        TypedConstantKind.Primitive, (int)MemorySafetyRulesVersion));
    AddSynthesizedAttribute(ref attributes, moduleBuilder.TrySynthesizeMemorySafetyRulesAttribute(version));
}
```

The **emitted version value is `2`**, not `15`:
`src/Compilers/Core/Portable/MemorySafetyRulesVersion.cs`

```csharp
[Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
public enum MemorySafetyRulesVersion
{
    /// <summary>Legacy rules.</summary>
    Version1 = 1,
    /// <summary>Updated rules introduced with the "unsafe evolution" language feature.</summary>
    Version2 = 2,
}
```

(`unsafe-evolution.md` still carries an open question "Value of `MemorySafetyRulesAttribute` —
What should be the 'enabled'/'updated' memory safety rules version? `2`? `15`? `11`?" and its
prose says `15`. **The implementation currently emits `2`.** This may still change before GA.)

Opt-in: `CSharpCompilationOptions.UseUpdatedMemorySafetyRules` (new public option), or the
legacy `Feature(Feature.UpdatedMemorySafetyRules)` feature flag
(`SourceModuleSymbol.MemorySafetyRulesVersion`, line 750).
`SourceModuleSymbol.AddMemorySafetyRulesAttributeIfNeeded` (line 313) calls
`DeclaringCompilation.EnsureMemorySafetyRulesAttributeExists(...)` during
`CompletionPart.StartValidatingReferencedAssemblies` and only reports diagnostics when
`OutputKind == OutputKind.NetModule`. The attribute is also **embeddable**
(`SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol.cs`).

Reading side: new public Roslyn API **`IModuleSymbol.MemorySafetyRulesVersion`**
(`src/Compilers/Core/Portable/Symbols/IModuleSymbol.cs`), implemented by
`PEModuleSymbol` (C# and VB), `SourceModuleSymbol`, `RetargetingModuleSymbol`,
`MissingModuleSymbol`. `PEUtilities.DeriveUnrecognizedMemorySafetyRulesAttributeDiagnostic`
reports `ERR_UnrecognizedAttributeVersion` when the version is neither 1 nor 2.
An assembly whose module carries version 2 promises that **every** member requiring an
unsafe context is annotated with `RequiresUnsafeAttribute`; consumers of a module without
the attribute fall back to the "compat mode" (a member is *requires-unsafe* if a pointer or
function-pointer type appears anywhere in its parameter or return types, excluding
substituted generic parameters and constraint types).

### 3.7 `ExtendedLayoutAttribute` and `TypeAttributes.ExtendedLayout`

**Both survive.** The attribute is a custom attribute (always kept) and
`TypeAttributes` is written into the TypeDef row unconditionally.

Values:

- `System.Reflection.TypeAttributes.ExtendedLayout = 0x00000018` (24)
  (`src/libraries/System.Private.CoreLib/src/System/Reflection/TypeAttributes.cs`;
  `System.Runtime.cs` ref line 14088 shows `ExtendedLayout = 24,`).
  **Beware: `LayoutMask = 0x00000018` has the same value.** `ExtendedLayout` is the fourth
  (previously reserved) value of the two-bit layout mask:
  `AutoLayout = 0x0`, `SequentialLayout = 0x8`, `ExplicitLayout = 0x10`, `ExtendedLayout = 0x18`.
  Any code written as `(flags & TypeAttributes.ExtendedLayout) != 0` is **wrong** — it also
  matches sequential and explicit layout. The correct test is
  `(flags & TypeAttributes.LayoutMask) == TypeAttributes.ExtendedLayout`.
- `System.Runtime.InteropServices.LayoutKind.Extended = 1` — a **new named member** occupying
  the previously unused value 1 (`System.Runtime.cs` ref line 15623:
  `Sequential = 0, Extended = 1, Explicit = 2, Auto = 3`).
- `System.Runtime.InteropServices.ExtendedLayoutKind { CStruct = 0, CUnion = 1 }`.
- `System.Runtime.InteropServices.ExtendedLayoutAttribute`:
  `[AttributeUsage(AttributeTargets.Struct, Inherited = false)] public sealed class ExtendedLayoutAttribute : Attribute { public ExtendedLayoutAttribute(ExtendedLayoutKind layoutKind) { } }`

ECMA-335 status: **merged into the augments document**,
`dotnet/runtime` `docs/design/specs/Ecma-335-Augments.md`, section "## Extended Layout"
(line 1149). Key clauses:

- I.9.5 adds the layout rule `extendedlayout`.
- II.10.1 adds `extended` as a `ClassAttr`; II.10.1.2 describes it.
- II.10.7: "The **.pack** and **.size** directives are not valid on a type marked with `extended`."
- II.22.8 diffs: "A type has layout if it is marked SequentialLayout or ExplicitLayout or
  ExtendedLayout." and "A type with ExtendedLayout **must immediately inherit from
  System.ValueType**." and "(That is, AutoLayout and ExtendedLayout types shall not own any
  rows in the ClassLayout table.)"
- II.22.37 removes the clause "b. can set 0 or 1 of `SequentialLayout` and `ExplicitLayout`…".
- II.23.1.15 adds the row
  `| ExtendedLayout | 0x00000018 | Layout is supplied by a System.Runtime.InteropServices.ExtendedLayoutAttribute custom attribute |`.

Roslyn behaviour — `docs/features/ExtendedLayoutAttribute.md`:

- If a type has `ExtendedLayoutAttribute`, the compiler emits `TypeAttributes.ExtendedLayout`.
- `StructLayoutAttribute` may not be combined with it; C# also forbids `InlineArrayAttribute`.
- `ITypeSymbol.Layout` returns a `TypeLayout` with `LayoutKind` = `Extended` (1),
  `Size` = 0, `Pack` = 0.
- NoPia-embedded types keep the attribute.
- "The Roslyn compiler will not have knowledge of the specific options available on the
  `ExtendedLayoutAttribute` … The compiler will not attempt to detect invalid field types."
- `AttributeDescription.ExtendedLayoutAttribute` uses
  `s_signature_HasThis_Void_ExtendedLayoutKind`, i.e. a single-argument constructor whose
  parameter is the `ExtendedLayoutKind` enum (`TypeHandleTargetInfo(interopServices,
  "ExtendedLayoutKind", SerializationTypeCode.Int32)`).

Status: `docs/Language Feature Status.md` lists `[ExtendedLayoutAttribute]` under **C# 15.0**
as "Merged into 18.3" (roslyn PR #78741), developer `jkoritzinsky`, tracking
`dotnet/runtime` issue #100896.

### 3.8 Extension-member lowering artifacts

The whole shape is built by
`src/Compilers/CSharp/Portable/Symbols/Source/ExtensionGroupingInfo.cs`.
Emitted metadata (from `dotnet/csharplang` `proposals/csharp-15.0/extension-indexers.md`,
"Metadata"):

```csharp
[Extension]
static class BitExtensions
{
    [Extension, SpecialName, DefaultMember("Item")]
    public sealed class <G>$T0 // grouping type
    {
        [SpecialName]
        public static class <M>$T_t // marker type
        {
            [SpecialName]
            public static void <Extension>$(T t) { } // marker method
        }

        [ExtensionMarkerName("<M>$T_t")]
        public bool this[int index] // extension indexer "skeleton"
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    // accessor implementation methods, on the enclosing static class
    public static bool get_Item<T>(T t, int index) => ...;
    public static void set_Item<T>(T t, int index, bool value) => ...;
}
```

(The proposal spells the attribute `[ExtensionMarkerName]`; the real type is
`System.Runtime.CompilerServices.ExtensionMarkerAttribute`.)

`System.Runtime.CompilerServices.ExtensionMarkerAttribute`
(`src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/ExtensionMarkerAttribute.cs`):

```csharp
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
              | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field
              | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
                Inherited = false)]
public sealed class ExtensionMarkerAttribute : Attribute
{
    public ExtensionMarkerAttribute(string name) => Name = name;
    public string Name { get; }
}
```

Roslyn: `AttributeDescription.ExtensionMarkerAttribute = new AttributeDescription(
"System.Runtime.CompilerServices", "ExtensionMarkerAttribute", s_signatures_HasThis_Void_String_Only)`.
Embeddable via `SynthesizedEmbeddedExtensionMarkerNameAttributeSymbol.cs`.

**Ref-assembly survival, artifact by artifact:**

| Artifact | Survives `/refonly`? | Why |
|---|---|---|
| Grouping type (`ExtensionGroupingType`) | **Always** | Emitted from `NamedTypeSymbolAdapter.GetNestedTypes` via `container.GetExtensionGroupingInfo().GetGroupingTypes()` with **no `EmitContext` filter**; and its `ITypeDefinitionMember.Visibility` is hard-coded `TypeMemberVisibility.Public`. `IsSpecialName => true`, `IsSealed => true`, `IsAbstract => true` (i.e. a static class), base type `System.Object`. |
| Marker type (`ExtensionMarkerType`) | **Always** | Returned from `ExtensionGroupingType.NestedTypes` with no filter; visibility `Public`; `IsAbstract`/`IsSealed` true. |
| Marker method (`SynthesizedExtensionMarker`, `<Extension>$`) | **Always, unconditionally** | `ExtensionMarkerType.GetMethods(EmitContext)` yields `UnderlyingExtensions[0].TryGetOrCreateExtensionMarker()` **without calling `ShouldInclude`**. Its declared visibility is `ExtensionGroupingInfo.GetCorrespondingMarkerMethodVisibility(...)` = the **maximum** metadata visibility over all members of the merged extension blocks (Public > Assembly > Private), but since no filter is applied it is emitted even when that visibility would fail `ShouldInclude`. |
| `[Extension]` and `[DefaultMember("Item")]` on the grouping type | **Always** | `ExtensionGroupingType.GetAttributes(EmitContext)` synthesizes both unconditionally; `DefaultMember` is added when any merged extension block contains an indexer, using `firstIndexer.MetadataName`. |
| Skeleton `Item` property (accessors `throw new NotImplementedException()`) | **Iff visible** | `ExtensionGroupingType.GetProperties(EmitContext)` uses `definition.ShouldInclude(context) \|\| !definition.GetAccessors(context).IsEmpty()`. A `public` extension indexer therefore survives, an `internal` one without IVT does not. |
| Skeleton accessor methods (`get_Item` / `set_Item` **on the grouping type**) | **Iff visible** | `ExtensionGroupingType.GetMethods(EmitContext)` applies `method.GetCciAdapter().ShouldInclude(context)`. They are **not virtual**, so only visibility saves them. |
| `[ExtensionMarker("<M>$…")]` on the skeleton members | **With the member** | Ordinary custom attribute. |
| Static implementation methods `get_Item` / `set_Item` **on the enclosing static class** | **Iff visible** | Ordinary `public static` methods of the enclosing static class (`SourceExtensionImplementationMethodSymbol`) — kept by `ShouldInclude` on visibility. For a `public` extension indexer they are public and survive. |
| `implAttributes` of any member inside an extension block | **Always zeroed** | `MethodSymbolAdapter.GetImplementationAttributes` returns `default` when `ContainingType.IsExtension` (§2.4). |
| Grouping-type type parameters | Renamed | `ExtensionGroupingTypeTypeParameter.Name => "$T" + Index`, all constraint attributes dropped, only a synthesized `IsUnmanagedAttribute` preserved. |

**Answer to the specific concern in the brief:** for a `public` extension indexer, *both*
the skeleton `Item` property (with its throwing accessors) *and* the static
`get_Item`/`set_Item` implementation methods survive into the reference assembly.
An extension indexer is therefore fully consumable from a ref-assembly-only reference.
They are dropped together (never one without the other) for `internal` members without
`InternalsVisibleTo`, which is the same behaviour as for any other internal member.

Reading side: `PEPropertySymbol` and `PEMethodSymbol` filter `ExtensionMarkerAttribute` out
of `GetAttributes()` when `this.IsExtensionBlockMember()` (see `PEPropertySymbol.cs` ~line 833:
`var filterExtensionMarkerAttribute = this.IsExtensionBlockMember();`).

---

## 4. Was there any change to ref-assembly rules in Roslyn 5.0 – 5.12?

**No.** Evidence:

1. `git log docs/features/refout.md` (via the GitHub commits API) — the ten commits ever made:

   | Date | SHA | Subject |
   |---|---|---|
   | 2024-02-21 | `418c56237a` | Fix broken link (#72173) |
   | 2021-06-17 | `c863e7545c` | Make it clear that method bodies aren't compiled when using `/refout` (#54128) |
   | 2020-03-16 | `2b8633892d` | Fix crash when compiling internal attribute constructor (#42192) |
   | 2019-01-15 | `50bb3faea4` | Resources should not be emitted into ref assemblies (#31244) |
   | 2018-08-20 | `233957b90f` | Add documentation for the `ProduceOnlyReferenceAssembly` msbuild option |
   | 2017-06-05 | `0f6948d20d` | msbuild change and determinism |
   | 2017-05-11 | `89ad231429` | Refout: Re-enable NoPia |
   | 2017-05-09 | `8af036a83a` | Refout: disallow NoPia, best-effort determinism, CopyRefAssembly |
   | 2017-05-02 | `32a9e283e2` | Add `.mvid` section to PE |
   | 2017-04-07 | `c371dc1627` | Document refout feature (#18501) |

   Nothing in the .NET 11 wave.

2. `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md` contains twelve entries
   (safe-context of Span collection expressions; `InAttribute` required for synthesized
   `ref readonly` delegates; the same for `ref readonly` local functions; dynamic `&&`/`||`
   with an interface-typed left operand; `nameof(this.)` in attributes; `with` parsing in
   switch-expression arms; `with()` as a collection-expression element; pointer types no
   longer require an unsafe context; `safe` contextual keyword; `unsafe` required for more
   members; `closed` contextual keyword; `union` contextual keyword) —
   **none of them concern reference assemblies**.

3. Grepping the emitter for `MetadataOnly` / `IsRefAssembly` / `IncludePrivateMembers`
   yields the same small set of call sites listed in §1.6, all pre-existing.

The "Future" section of `refout.md` still lists the same never-implemented refinements
(further reduce metadata under `/refout` to match `/refonly`; public-only ref assemblies
ignoring `InternalsVisibleTo`; `EmitOptions.TolerateErrors`; filtered XML documentation).

---

## 5. `ClosedDerivedTypeInfo.IsComplete` — what `false` actually means

### 5.1 The public API

`src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs`:

```csharp
/// <summary>Information about derived types of a closed type.</summary>
public readonly struct ClosedDerivedTypeInfo
{
    /// <summary>Possible direct derived types of the closed type.</summary>
    public ImmutableArray<INamedTypeSymbol> ClosedDerivedTypes { get; }

    /// <summary>
    /// Indicates whether <see cref="ClosedDerivedTypes" /> represents all possible derived types
    /// (i.e. it is a complete set).
    /// This will be false, for example, when a generic closed type has an unspeakable derived type.
    /// </summary>
    public bool IsComplete { get; }
}
```

`src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs`:

```csharp
/// <summary>Indicates that the type is restricted from being inherited from outside its containing module.</summary>
bool IsClosed { get; }

/// <summary>Gets the direct derived types of a closed type.</summary>
/// <exception cref="InvalidOperationException">If this is not a closed type.</exception>
ClosedDerivedTypeInfo GetClosedDerivedTypeInfo(CancellationToken cancellationToken);
```

`src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs` (line 222):

```csharp
ClosedDerivedTypeInfo ITypeSymbol.GetClosedDerivedTypeInfo(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (UnderlyingTypeSymbol is not Symbols.NamedTypeSymbol { IsClosed: true } namedType)
        throw new InvalidOperationException(CSharpResources.GetClosedDerivedTypeInfoMustBeClosed);

    var isComplete = namedType.TryGetClosedSubtypes(out var subtypes, cancellationToken);
    return new ClosedDerivedTypeInfo(subtypes.GetPublicSymbols(), isComplete);
}
```

### 5.2 The precise definition of `IsComplete`

`src/Compilers/CSharp/Portable/Symbols/NamedTypeSymbol.cs`, line 755:

```csharp
/// <remarks>
/// When a closed class contains type parameters, it's possible that some subtype may or
/// may not apply, depending on what type substitution is ultimately performed at a later stage.
/// This call will return false and only the subtypes which are speakable in terms of type
/// parameters on this in that situation.
/// </remarks>
internal bool TryGetClosedSubtypes(out ImmutableArray<NamedTypeSymbol> subtypes, CancellationToken cancellationToken = default)
{
    if (!IsClosed) { subtypes = []; return false; }
    ...
    (bool, ImmutableArray<NamedTypeSymbol>) calculateClosedSubtypes(CancellationToken cancellationToken)
    {
        var candidateSubtypes = CandidateClosedSubtypeDefinitions;
        if (!IsGenericType && candidateSubtypes.All(subtype => !subtype.IsGenericType))
            return (true, candidateSubtypes);           // <-- always complete in the non-generic case

        var resultBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance(candidateSubtypes.Length);
        var baseTypeTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
        this.FindTypeParameters(baseTypeTypeParameters);
        var success = tryGetSpeakableSubtypes(this, candidateSubtypes, resultBuilder, baseTypeTypeParameters, cancellationToken);
        baseTypeTypeParameters.Free();
        return (success, resultBuilder.ToImmutableAndFree());
    }

    static bool tryGetSpeakableSubtypes(NamedTypeSymbol @this, ImmutableArray<NamedTypeSymbol> candidateSubtypes,
        ArrayBuilder<NamedTypeSymbol> resultBuilder, HashSet<TypeParameterSymbol> baseTypeTypeParameters, CancellationToken ct)
    {
        bool allSubtypesAreSpeakable = true;
        foreach (var candidateSubtype in candidateSubtypes)
        {
            ct.ThrowIfCancellationRequested();
            if (TypeUnification.TryUnifyClosedSubtype(candidateSubtype, closedType: @this) is { } unifiedSubtype)
            {
                if (unifiedSubtype.IsGenericType &&
                    unifiedSubtype.ContainsAdditionalTypeParameter(allowedTypeParameters: baseTypeTypeParameters))
                {
                    // If 'unifiedSubtype' contains type parameters which are not present in '@this',
                    // it implies 'unifiedSubtype' was able to unify but is not speakable at the use site.
                    allSubtypesAreSpeakable = false;
                    continue;
                }
                resultBuilder.Add(unifiedSubtype);
            }
        }
        return allSubtypesAreSpeakable;
    }
}
```

So, exactly:

- `IsComplete == true` ⟺ every candidate direct subtype that unifies with the (possibly
  substituted) closed type is **speakable**, i.e. it introduces no type parameter beyond
  those appearing on the closed type itself. This is **always** the case when neither the
  closed type nor any candidate subtype is generic (early return `(true, candidateSubtypes)`).
- `IsComplete == false` ⟺ at least one candidate subtype unified but was **discarded**
  because it mentions a type parameter that the closed type does not. Canonical example:

  ```csharp
  closed class Base<T> { }
  sealed class Derived<T, U> : Base<T> { }   // U is not speakable from Base<T>
  ```

  `Base<int>.GetClosedDerivedTypeInfo()` yields `ClosedDerivedTypes` without `Derived<int, ?>`
  and `IsComplete == false`. The practical consequence is that exhaustiveness checking over
  the returned set is unsound and the compiler must not treat a switch over
  `ClosedDerivedTypes` as exhaustive.

- Candidate subtypes are **not** filtered by accessibility anywhere in this path.
- There is also a silent-failure path: `PENamedTypeSymbol.CandidateClosedSubtypeDefinitions`
  swallows `BadImageFormatException` / `UnsupportedSignatureContent` and returns whatever it
  gathered, with an acknowledged TODO
  (`https://github.com/dotnet/roslyn/issues/83617`: "It seems like we don't know what the
  candidate subtypes are in this case, so, perhaps we should not allow exhausting the type
  via its subtypes."). In that case `IsComplete` can still be `true` while the set is short.
  This is a **known open defect**, not the documented meaning of `IsComplete`.

### 5.3 Reference assembly vs implementation assembly

`src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PENamedTypeSymbol.cs`:

```csharp
internal sealed override bool IsClosed
{
    get
    {
        ...
        var hasIsClosedTypeAttribute = ContainingPEModule.Module.HasAttribute(_handle, AttributeDescription.IsClosedTypeAttribute);
        uncommon.lazyIsClosed = hasIsClosedTypeAttribute.ToThreeState();
        return hasIsClosedTypeAttribute;
    }
}

internal sealed override ImmutableArray<NamedTypeSymbol> CandidateClosedSubtypeDefinitions
{
    get
    {
        if (!IsClosed) return [];
        ...
        ImmutableArray<NamedTypeSymbol> findClosedSubtypes()
        {
            var metadataReader = ContainingPEModule.Module.MetadataReader;
            var decoder = new MetadataDecoder(ContainingPEModule);
            var thisTypeIsGeneric = IsGenericType;
            foreach (var candidateTypeDefHandle in metadataReader.TypeDefinitions)   // <-- scans EVERY TypeDef
            {
                var typeDef = metadataReader.GetTypeDefinition(candidateTypeDefHandle);
                var baseTypeHandle = typeDef.BaseType;
                // fast path for TypeDef / TypeRef base handles, then
                // GetTypeSpecificationSignatureReaderOrThrow(...) + ReadSignatureTypeCode() == GenericTypeInstance
                // then a full symbol decode + BaseTypeNoUseSiteDiagnostics.OriginalDefinition.Equals(this, TypeCompareKind.CLRSignatureCompareOptions)
            }
        }
    }
}
```

Two facts settle the question:

1. **Roslyn never reads `IsClosedTypeAttribute.DerivedTypes`.**
   Only the *presence* of the attribute is tested (`HasAttribute(...)`).
   The derived set is recomputed by scanning `metadataReader.TypeDefinitions`.
   (`AttributeDescription.IsClosedTypeAttribute` even declares only the parameterless
   constructor signature.) So any imagined discrepancy between the attribute's array and
   reality cannot influence `GetClosedDerivedTypeInfo`.
2. **Reference assemblies keep every TypeDef row.** `refout.md`: "all types (including
   private or nested types) are kept in ref assemblies". The only type-level omission on the
   metadata-only path is anonymous types (`PEModuleBuilder.GetAnonymousTypeDefinitions`
   returns empty when `context.MetadataOnly`), plus lowering-produced closure / state-machine
   types which never come into existence because method bodies are not compiled. Neither can
   derive from a user-declared closed class.

**Therefore: `ITypeSymbol.GetClosedDerivedTypeInfo` returns the same `ClosedDerivedTypes`
set and the same `IsComplete` value whether the closed class is read from the reference
assembly or from the implementation assembly.**

Caveat worth recording: because the scan is over the *module of the referenced assembly*,
a consumer that references only the ref assembly gets the derived types as
`PENamedTypeSymbol`s from that ref assembly. Their members are the ref-assembly-filtered
members (no private/internal function members). That affects what you can *do* with the
derived types, not the identity or completeness of the set.

---

## 6. Where each attribute lives in Roslyn's tables (quick index)

`src/Compilers/Core/Portable/Symbols/Attributes/AttributeDescription.cs`:

```csharp
line 487: MemorySafetyRulesAttribute   = ("System.Runtime.CompilerServices", "MemorySafetyRulesAttribute",   s_signatures_HasThis_Void_Int32_Only)
line 488: RequiresUnsafeAttribute      = ("System.Diagnostics.CodeAnalysis",  "RequiresUnsafeAttribute",      s_signatures_HasThis_Void_Only)
line 501: ExtensionMarkerAttribute     = ("System.Runtime.CompilerServices", "ExtensionMarkerAttribute",     s_signatures_HasThis_Void_String_Only)
line 502: RuntimeAsyncMethodGenerationAttribute = ("System.Runtime.CompilerServices", "RuntimeAsyncMethodGenerationAttribute", s_signatures_HasThis_Void_Boolean_Only)
line 504: ExtendedLayoutAttribute      = ("System.Runtime.InteropServices",  "ExtendedLayoutAttribute",      s_signaturesOfExtendedLayoutAttribute)
line 505: UnionAttribute               = ("System.Runtime.CompilerServices", "UnionAttribute",               s_signatures_HasThis_Void_Only)
line 506: IsClosedTypeAttribute        = ("System.Runtime.CompilerServices", "IsClosedTypeAttribute",        s_signatures_HasThis_Void_Only)
```

`src/Compilers/Core/Portable/WellKnownTypes.cs` (enum members and metadata names):

```
System_Runtime_CompilerServices_MemorySafetyRulesAttribute
System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute
System_Runtime_CompilerServices_IsClosedTypeAttribute
System_Runtime_CompilerServices_ExtensionMarkerAttribute
System_Runtime_CompilerServices_UnionAttribute
System_Runtime_CompilerServices_IUnion
```

`src/Compilers/Core/Portable/WellKnownMember.cs`:

```
System_Runtime_CompilerServices_MemorySafetyRulesAttribute__ctor
System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute__ctor
System_Runtime_CompilerServices_IsClosedTypeAttribute__ctor
System_Runtime_CompilerServices_IsClosedTypeAttribute__DerivedTypes
System_Runtime_CompilerServices_ExtensionMarkerAttribute__ctor
System_Runtime_CompilerServices_UnionAttribute__ctor
```

Embeddable (compiler-synthesized when the corlib lacks them) —
`src/Compilers/CSharp/Portable/Symbols/EmbeddableAttributes.cs` +
`src/Compilers/CSharp/Portable/Emitter/Model/PEAssemblyBuilder.cs`:

- `SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol.cs`
- `SynthesizedEmbeddedExtensionMarkerNameAttributeSymbol.cs`
- `RequiresUnsafeAttribute` (via `EmbeddableAttributes`)
- `IsClosedTypeAttribute` is polyfilled too — System.Text.Json's comment states:
  "When targeting a runtime that predates `System.Runtime.CompilerServices.IsClosedTypeAttribute`,
  the C# compiler polyfills the attribute directly into the consuming assembly."

All of these embedded types are internal + `[Microsoft.CodeAnalysis.Embedded]`, and all of
them survive into ref assemblies because ref assemblies keep all types and all attributes
including internal ones and their internal constructors.

---

## 7. Exact numeric values summary

| Symbol | Namespace / type | Value (hex) | Value (dec) | Named member in .NET 11? | ECMA-335 |
|---|---|---|---|---|---|
| `Async` | `System.Reflection.MethodImplAttributes` | `0x2000` | 8192 | **Yes** (`System.Runtime.cs` line 13681) | **Draft only** (`docs/design/specs/runtime-async.md`, II.23.1.11) |
| `Async` | `System.Runtime.CompilerServices.MethodImplOptions` | `0x2000` | 8192 | **Yes** (`System.Runtime.cs` line 15014) | same |
| `ExtendedLayout` | `System.Reflection.TypeAttributes` | `0x00000018` | 24 | **Yes** (`System.Runtime.cs` line 14088) | **Merged** into `Ecma-335-Augments.md`, II.23.1.15 |
| `LayoutMask` | `System.Reflection.TypeAttributes` | `0x00000018` | 24 | pre-existing | — |
| `Extended` | `System.Runtime.InteropServices.LayoutKind` | `1` | 1 | **Yes** (`System.Runtime.cs` line 15625) | — |
| `CStruct` / `CUnion` | `System.Runtime.InteropServices.ExtendedLayoutKind` | `0` / `1` | 0 / 1 | **Yes** | — |
| `MemorySafetyRulesAttribute` version argument | — | — | **2** (Roslyn `MemorySafetyRulesVersion.Version2`) | — | — |
| `RefSafetyRulesAttribute` version argument | — | — | 11 | pre-existing | — |
| `ClosedClasses` | `CompilerFeatureRequiredFeatures` (Roslyn-internal) | `1 << 3` | 8 | metadata string `"ClosedClasses"` | — |
| CoreCLR type system `MethodFlags.Async` | `EcmaMethod` (private) | `0x02000` | 8192 | — | — |

**Neither `System.Reflection.Metadata` nor any other assembly defines a *separate*
named member for these two flags.** SRM surfaces them through
`System.Reflection.TypeAttributes` / `System.Reflection.MethodImplAttributes`
(`TypeDefinition.Attributes`, `MethodDefinition.ImplAttributes`), which come from
whichever `System.Runtime` the consumer compiles against.

---

## 8. `System.Reflection.Metadata` in .NET 11

### 8.1 Public API surface: unchanged

`src/libraries/System.Reflection.Metadata/ref/System.Reflection.Metadata.cs` — last commit
**2025-07-01**, `544995bb4a`, "[SRM] Add APIs to get the `AssemblyNameInfo` of an assembly
definition or reference. (#116839)" — a **.NET 10** change.
There is **no** .NET 11 public-API change to `MetadataReader`, `MetadataBuilder`,
`PEReader`, `PEBuilder`, `ManagedPEBuilder`, `BlobBuilder`, `BlobEncoder`, or any
signature encoder. The absence noted in the inventory is now **confirmed**, not merely
undocumented.

### 8.2 Behavioural changes since .NET 10 GA (26 commits since 2025-11-01)

The ones that matter for anything that *writes* IL or metadata:

| Date | PR | Change |
|---|---|---|
| 2026-06-26 | #129626 | Remove unsafe code from `System.Reflection.Metadata` PE/blob writers |
| 2026-06-15 | **#128279** | **Fix incorrect operand size for long-form IL local/argument instructions in `InstructionEncoder`** (`ldloc`/`stloc`/`ldarg`/`starg` long forms) |
| 2026-04-27 | **#127262** | **Fix SRM branch fixup skipping bytes at `BlobBuilder` chunk boundaries** |
| 2026-04-24 | **#127246** | `System.Reflection.Metadata`: preserve pre-linked suffix chain when linking into an empty `BlobBuilder` |
| 2026-04-24 | #127308 | [SRM] Miscellaneous clean-up |
| 2026-04-19 | **#126924** | **Clear blob returned by `BlobBuilder.ReserveBytes`** (previously could return uninitialized bytes) |
| 2026-03-30 | #126280 | Remove unsafe code from `BlobUtilities.cs` |
| 2026-03-22 | #121223 | [SRM] Optimize `MetadataBuilder.GetOrAddConstantBlob` |
| 2026-01-15 | #123180 | Remove `Debug.Assert` from metadata entity struct constructors |
| 2025-12-01 | **#115268** | **Fix `MetadataAggregator`: remove cumulative sum for GUID-heap offset** |
| 2026-05-21 | #128389 | Replace small `stackalloc`s with collection literals |
| 2026-07-28 / 2026-07-14 / 2026-07-09 / 2026-06-30 / 2026-06-03 / 2026-05-08 / 2026-04-25 / 2026-04-21 / 2026-04-01 ×2 / 2026-03-15 / 2026-02-20 ×2 / 2026-02-01 / 2025-11-17 | — | test/infrastructure/polyfill/refactor only |

The four bolded correctness fixes (#128279, #127262, #127246, #126924, #115268) are the ones
a metadata-writing tool should care about: three of them are `BlobBuilder`/`InstructionEncoder`
bugs that could corrupt emitted IL, and #115268 is a `MetadataAggregator` (EnC delta) fix.

### 8.3 What SRM does *not* do

- It does not validate `MethodImplAttributes` bits; `MethodDefinition.ImplAttributes` is a
  straight cast of the raw `ushort`.
- It does not validate `TypeAttributes` layout bits; `TypeDefinition.Attributes` is a
  straight cast of the raw `uint`.
- `MetadataBuilder.AddMethodDefinition(MethodAttributes, MethodImplAttributes, ...)` accepts
  `(MethodImplAttributes)0x2000` without complaint.
- Consequently SRM **reads and writes both new flags correctly today**, on every target
  framework, including netstandard2.0 — only the *named* enum member requires .NET 11.

---

## 9. ILVerify

- Source: `dotnet/runtime` `src/coreclr/tools/ILVerification` (the library) and
  `src/coreclr/tools/ILVerify` (the CLI: `ILVerify.csproj`, `Program.cs`,
  `ILVerifyRootCommand.cs`, `README.md`).
- **Distribution: the `dotnet-ilverify` NuGet global tool, not the SDK.**
  (`nuget.org/packages/dotnet-ilverify`, latest listed release 10.0.3.) There is no
  ILVerify binary in the .NET SDK layout, and no `ilverify` reference in `dotnet/sdk`
  (code search over `dotnet/sdk` for `ilverify` returns 0 hits).
- **`dotnet/runtime` PR #121503 — "Update ILVerify to honor the *async* flag" — merged
  2025-11-13**, PR body: "Runtime specification:
  https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md.
  Note: ilasm/ildasm were already [updated](https://github.com/dotnet/runtime/pull/115658)
  to recognize the 'async' keyword. Relates to test plan
  https://github.com/dotnet/roslyn/issues/75960."

The code, `src/coreclr/tools/ILVerification/ILImporter.Verify.cs`, `ImportReturn` (~line 1875):

```csharp
var declaredReturnType = _method.Signature.ReturnType;

// For async methods, unwrap Task/ValueTask return types
TypeDesc expectedReturnType = declaredReturnType;
if (_method.IsAsync)
{
    if (IsTaskOrValueTaskType(declaredReturnType, out TypeDesc unwrappedType))
        expectedReturnType = unwrappedType;
    else
        VerificationError(VerifierError.StackUnexpected);   // Async methods must return Task or ValueTask
}

if (expectedReturnType.IsVoid)
{
    if (_stackTop > 0) VerificationError(VerifierError.ReturnVoid, _stack[_stackTop - 1]);
}
else
{
    if (_stackTop <= 0) VerificationError(VerifierError.ReturnMissing);
    else
    {
        Check(_stackTop == 1, VerifierError.ReturnEmpty);
        var actualReturnType = Pop();
        CheckIsAssignable(actualReturnType, StackValue.CreateFromType(expectedReturnType));
        Check((!expectedReturnType.IsByRef && !expectedReturnType.IsByRefLike) || actualReturnType.IsPermanentHome,
              VerifierError.ReturnPtrToStack);
    }
}
```

with

```csharp
bool IsTaskOrValueTaskType(TypeDesc type, out TypeDesc unwrappedType)
{
    // namespace must be "System.Threading.Tasks"
    // "Task" / "ValueTask" non-generic  -> WellKnownType.Void
    // "Task`1" / "ValueTask`1"          -> Instantiation[0]
}
```

and `EcmaMethod.IsAsync` (`src/coreclr/tools/Common/TypeSystem/Ecma/EcmaMethod.cs`):

```csharp
private static class MethodFlags { ...; public const int Async = 0x02000; }
...
if ((methodImplAttributes & MethodImplAttributes.Async) != 0) flags |= MethodFlags.Async;
...
public override bool IsAsync => (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.Async) & MethodFlags.Async) != 0;
```

**Answer to question 6: yes.** With `MethodImplAttributes.Async` set, ILVerify expects a
`ret` whose stack shape matches the *unwrapped* type argument (or an empty stack for
non-generic `Task`/`ValueTask`), i.e. exactly the runtime-async convention, and reports
`StackUnexpected` if the declared return type is not one of the four Task-like types.
Without the flag it enforces the ordinary rule. There is therefore no scenario in which
ILVerify rejects a correctly-emitted runtime-async body, and no scenario in which it
accepts a runtime-async convention on a method that lacks the flag.

Later ILVerification changes in the .NET 11 window, for completeness:
`f44c762122` (2026-05-18, #122056, `IndexOutOfRangeException` for malformed exception-handling
clause bounds), `76610ec81a` (2026-06-08, Utf8Span for type-system names),
`cdc5b6220b` (2026-08-12, #129118, validate invalid base-type declarations).

---

## 10. Loose ends / things worth watching before GA

1. **`MemorySafetyRulesAttribute` version value is still open.** `unsafe-evolution.md`
   prose says `15`; `MemorySafetyRulesVersion.Version2 = 2` is what Roslyn emits and what
   `ERR_UnrecognizedAttributeVersion` reports. The proposal's open question
   "Value of `MemorySafetyRulesAttribute` — `2`? `15`? `11`?" is unresolved.
2. **Runtime async is preview at GA.** `docs/Language Feature Status.md`:
   "Runtime Async | main | Main feature merged into main in preview"; a separate
   `runtime-async-streams` branch is still in progress. `MethodImplAttributes.Async`
   requires `/features:runtime-async=on` or `[RuntimeAsyncMethodGeneration(true)]`.
   The `runtime-async.md` ECMA draft is explicitly "not yet official".
3. **`IsClosedTypeAttribute.DerivedTypes` is currently write-only from Roslyn's point of
   view.** Roslyn emits it and never reads it; the only confirmed reader is
   System.Text.Json's polymorphism inference. If Roslyn later starts reading it (e.g. to
   avoid the full TypeDef scan), the ref-assembly answer in §5.3 would need re-checking,
   because the scan-based and attribute-based answers could then diverge for cross-module
   scenarios.
4. **`PENamedTypeSymbol.CandidateClosedSubtypeDefinitions` silently swallows
   `BadImageFormatException`/`UnsupportedSignatureContent`** and can therefore return an
   incomplete set with `IsComplete == true` — tracked as
   <https://github.com/dotnet/roslyn/issues/83617>.
5. **`MethodSymbolAdapter.GetImplementationAttributes` zeroes impl attributes for all
   members of an extension block.** This is deliberate but means the `Async` bit (and
   `AggressiveInlining`, `NoInlining`, … ) never appears on an extension skeleton member,
   only on its implementation method.
6. **`TypeAttributes.ExtendedLayout == TypeAttributes.LayoutMask == 0x18`.** Any existing
   bit-test written as `(attrs & TypeAttributes.ExtendedLayout) != 0` silently
   misclassifies sequential and explicit layout. Mask-and-compare is mandatory.
7. **`Mono.Cecil` has no `Async` member** (as of `jbevain/cecil` `master`), though it
   round-trips the bit. Anything that reconstructs `MethodImplAttributes` from named
   members rather than the raw value will lose the flag.

---

## 11. Source URLs (all verified 2026-09-03)

Roslyn
- <https://github.com/dotnet/roslyn/blob/main/docs/features/refout.md>
- <https://github.com/dotnet/roslyn/blob/main/docs/features/ExtendedLayoutAttribute.md>
- <https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>
- <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%2011.md>
- <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Runtime%20Async%20Design.md>
- <https://github.com/dotnet/roslyn/blob/main/eng/Versions.props>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/PEWriter/MetadataWriter.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/PEWriter/Members.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Emit/Context.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Emit/CommonPEModuleBuilder.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/MethodImplExtensions.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Compilation/ClosedDerivedTypeInfo.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/IMethodSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/IModuleSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/CompilerFeatureRequiredFeatures.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/MemorySafetyRulesVersion.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/Attributes/AttributeDescription.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/WellKnownTypes.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/WellKnownMember.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Emitter/Model/PEModuleBuilder.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Emitter/Model/NamedTypeSymbolAdapter.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Emitter/Model/MethodSymbolAdapter.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/NamedTypeSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/MethodSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PENamedTypeSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEMethodSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEPropertySymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEUtilities.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceNamedTypeSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceModuleSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceMethodSymbol.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceMethodSymbolWithAttributes.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Source/ExtensionGroupingInfo.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedInstanceConstructor.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs>
- <https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Test/CSharp15/ClosedClassesTests.cs>

dotnet/runtime
- <https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md>
- <https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Reflection/MethodImplAttributes.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Reflection/TypeAttributes.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/MethodImplOptions.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IsClosedTypeAttribute.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/UnionAttribute.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IUnion.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/MemorySafetyRulesAttribute.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/ExtensionMarkerAttribute.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/RequiresUnsafeAttribute.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/ExtendedLayoutAttribute.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/ExtendedLayoutKind.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime/ref/System.Runtime.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/ref/System.Reflection.Metadata.cs>
- <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/DefaultJsonTypeInfoResolver.Helpers.cs>
- <https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/ILVerification/ILImporter.Verify.cs>
- <https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/Common/TypeSystem/Ecma/EcmaMethod.cs>
- <https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/ILVerify/README.md>
- <https://github.com/dotnet/runtime/pull/121503> (ILVerify honors the async flag)
- <https://github.com/dotnet/runtime/pull/115658> (ilasm/ildasm recognize `async`)
- <https://github.com/dotnet/runtime/issues/100896> (ExtendedLayoutAttribute)
- <https://github.com/dotnet/runtime/issues/114310> (Public API for Runtime Async)

dotnet/csharplang
- <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md>
- <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md>
- <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/extension-indexers.md>
- <https://github.com/dotnet/csharplang/blob/main/proposals/unsafe-evolution.md>

dotnet/sdk
- <https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.TargetFrameworkInference.targets>

Other
- <https://github.com/jbevain/cecil/blob/master/Mono.Cecil/MethodImplAttributes.cs>
- <https://github.com/jbevain/cecil/blob/master/Mono.Cecil/AssemblyReader.cs>
- <https://www.nuget.org/packages/dotnet-ilverify/>
- <https://learn.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies>

---

## 12. Addendum — evidence from `ClosedClassesTests.cs`, and attribute filtering in the code model

Source: `src/Compilers/CSharp/Test/CSharp15/ClosedClassesTests.cs` (7823 lines).

### 12.1 The emitted IL for a closed class (test `Symbols_01`)

```il
.class private auto ansi abstract beforefieldinit C
    extends [System.Runtime]System.Object
{
    .custom instance void System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
        01 00 01 00 54 1d 50 0c 44 65 72 69 76 65 64 54
        79 70 65 73 00 00 00 00
    )
    .method family hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 0d 43 6c 6f 73 65 64 43 6c 61 73 73 65 73
            00 00
        )
        ...
    }
}
```

Blob decoding of the `IsClosedTypeAttribute` application:
`01 00` prolog, `01 00` = one named argument, `54` = `PROPERTY`, `1d` = `SZARRAY`,
`50` = `System.Type` element, `0c` = 12-character name `"DerivedTypes"`,
`00 00 00 00` = an array of length 0.
So the named argument is **always written**, even when empty, whenever the well-known
property is available.

The `CompilerFeatureRequiredAttribute` blob is `01 00`, `0d` = 13-character UTF-8 string
`"ClosedClasses"`, `00 00`.

### 12.2 Internal derived types ARE recorded

Test `DerivedTypesMetadata_01`:

```csharp
closed class C;
class D1 : C;   // implicitly internal
class D2 : C;   // implicitly internal
class D3 : D1;
```

asserts

```
"System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {typeof(D1), typeof(D2)})"
```

Both `D1` and `D2` are **internal** (no accessibility modifier on a top-level type) and both
appear. This **definitively refutes** the hypothesis that `DerivedTypes` omits internal
derived types and that this is what `IsComplete == false` means.
Note also that only **direct** derived types are recorded: `D3 : D1` does not appear on `C`.

Other observed shapes:

- `DerivedTypesMetadata_03`: `{typeof(D1), typeof(D2), typeof(D3<>), typeof(D4<>), typeof(D5<,>)}`
  — generic derived types are recorded as **unbound generic definitions**
  (`GetUnboundGenericTypeOrSelf()`).
- `DerivedTypesMetadata_04`: `{typeof(Container<>.D1<>), typeof(Container<>.D2)}`
  — nested derived types are recorded with the containing type's unbound form.

### 12.3 New diagnostic: `ERR_ClosedBadDerivedTypesProperty` = **CS9395**

> "'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance
> property with public get and set accessors, no parameters, and type 'System.Type[]'."

Reported when a hand-rolled / polyfilled `IsClosedTypeAttribute` declares a `DerivedTypes`
property with the wrong shape (getter-only, setter-only, non-public accessor, wrong type).
If the property is **absent entirely**, no diagnostic is reported and the compiler emits the
attribute with only its parameterless constructor
(`DerivedTypesMetadata_06`, matching the `derivedTypesProperty is not null` branch in
`SourceNamedTypeSymbol.AddSynthesizedAttributes`). This matters for down-level targets where
the attribute is polyfilled.

### 12.4 Attributes filtered out of the Roslyn code model

`Symbols_01` asserts, for both the source compilation and the metadata (PE) compilation:

```csharp
Assert.True(classC.IsClosed);
// IsClosedTypeAttribute is filtered out of source and metadata symbols.
Assert.Empty(classC.GetAttributes());

var ctor = classC.Constructors.Single();
// CompilerFeatureRequiredAttribute is filtered out
Assert.Empty(ctor.GetAttributes());
```

while the raw metadata (via `peModule.GetCustomAttributesForToken(peType.Handle)`) still
shows `IsClosedTypeAttribute(DerivedTypes = {})`.

This is the same pattern used for `IsReadOnlyAttribute`, `RequiredMemberAttribute`,
`ExtensionMarkerAttribute` and `RequiresUnsafeAttribute`: the attribute is **not** visible
through `ISymbol.GetAttributes()`; the information is surfaced as a symbol property instead
(`ITypeSymbol.IsClosed`, `IMethodSymbol` diagnostics, `IModuleSymbol.MemorySafetyRulesVersion`, …).
See `PEPropertySymbol.cs` ~line 823–860 for the filter loop that sets
`hasRequiredMemberAttribute`, `hasRequiresUnsafeAttribute` and drops
`ExtensionMarkerAttribute` when `IsExtensionBlockMember()`.

The practical consequence for any tool that builds its own code model on Roslyn: these new
attributes must be read either from a dedicated `ISymbol` property (where one exists) or
from raw metadata, never from `GetAttributes()`.

### 12.5 No ref-assembly test coverage for closed classes

`ClosedClassesTests.cs` contains **no** occurrence of `refonly`, `refout`,
`EmitMetadataOnly` or `IncludePrivateMembers`. The conclusion in §5.3 is derived from the
emitter code, not from an existing Roslyn test. Anyone depending on it should consider
writing a confirming test.
