# C# 15 / .NET 11 — Memory safety and "unsafe evolution"

Research date: 2026-09-03. All facts below were verified against primary sources
(dotnet/csharplang, dotnet/roslyn `main`, dotnet/runtime `main`, dotnet/designs, dotnet/core
release notes, learn.microsoft.com). Roslyn source was read from `main` at the time of research.

## 0. Executive summary (the load-bearing facts)

1. **The whole feature is PREVIEW at .NET 11 GA.** `MessageID.IDS_FeatureUnsafeEvolution`
   maps to `LanguageVersion.Preview`, listed in Roslyn under the comment
   `// C# preview features.` — *not* under `// C# 15.0 features.`
   `LanguageVersion.CSharp15 = 1500` is the highest concrete version; there is no `CSharp16`
   enum member, and `LanguageVersionFacts.CurrentVersion => LanguageVersion.CSharp15`.
   Therefore **nothing** in this feature is active at `LangVersion=15` / `latest` / default.
2. **Two independent switches**, both required for the full model:
   - `<LangVersion>preview</LangVersion>` — enables the pointer relaxations, `unsafe(expr)`
     expressions and the `safe` contextual keyword *parsing*.
   - `<Features>$(Features);updated-memory-safety-rules</Features>` — enables the
     *requires-unsafe* member model and stamps `MemorySafetyRulesAttribute(2)` on the module.
     There is **no** `/memorysafetyrules:` command-line switch and **no** `<MemorySafetyRules>`
     MSBuild property in .NET 11; those are planned for .NET 12/13.
3. **The pointer relaxations are gated on LangVersion only, not on the assembly opt-in.**
   Compiling with `LangVersion=preview` makes pointer declaration, `&`, `fixed`, `stackalloc`-to-
   pointer and `sizeof` legal in safe code *whether or not* the assembly opted in.
4. **New Roslyn public API is `[Experimental("RSEXPERIMENTAL006")]`**, tracked by
   **roslyn issue #82789** ("Unsafe Evolution public API", still open). Any consumer must
   suppress `RSEXPERIMENTAL006` to compile against it.
5. New syntax: `UnsafeExpressionSyntax` (`SyntaxKind.UnsafeExpression = 8769`) and
   `SyntaxKind.SafeKeyword = 8454` (a contextual keyword, now the *last* contextual keyword).
6. The BCL has **not** been annotated. `RequiresUnsafeAttribute` and `MemorySafetyRulesAttribute`
   exist in dotnet/runtime `main` and in the `System.Runtime` reference assembly, but no BCL
   member carries `[RequiresUnsafe]` and the runtime build does not set the feature flag.

## 1. Primary sources

| Source | URL |
|---|---|
| Feature specification (speclet) | https://github.com/dotnet/csharplang/blob/main/proposals/unsafe-evolution.md |
| Champion issue | https://github.com/dotnet/csharplang/issues/9704 |
| Roslyn **public API tracking issue 82789** | https://github.com/dotnet/roslyn/issues/82789 |
| Roslyn test plan / status | https://github.com/dotnet/roslyn/issues/81207 |
| Roslyn feature branch | https://github.com/dotnet/roslyn/tree/features/UnsafeEvolution |
| Roslyn merge PR | https://github.com/dotnet/roslyn/pull/82547 |
| Roslyn Language Feature Status | https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md |
| Roslyn breaking changes (.NET 11) | https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%2011.md |
| Ecosystem design: caller-unsafe | https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/caller-unsafe.md |
| Ecosystem design: SDK adoption | https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/sdk-memory-safety-enforcement.md |
| Docs: What's new in C# 15 (ms.date 2026-08-14, updated 2026-08-19) | https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15 |
| Docs: Unsafe code (has "The updated memory safety model (preview)") | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code |
| .NET blog "Improving C# Memory Safety" (2026-05-21) | https://devblogs.microsoft.com/dotnet/improving-csharp-memory-safety/ |
| .NET 11 Preview 5 C# notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/csharp.md |
| .NET 11 Preview 7 C# notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/csharp.md |
| LDM notes 2025-11-05, 2025-11-12, 2026-01-26, 2026-04-01/06/13/22/29, 2026-05-13/27, 2026-07-01/22, 2026-08-05 | https://github.com/dotnet/csharplang/tree/main/meetings |
| Spec change PR | https://github.com/dotnet/csharplang/pull/10091 |
| BCL attribute PR | https://github.com/dotnet/runtime/pull/125721 |

## 2. Status at .NET 11 GA (November 2026)

### 2.1 Roslyn language-version gating (definitive)

`src/Compilers/CSharp/Portable/Errors/MessageID.cs`:

```csharp
internal static LanguageVersion RequiredVersion(this MessageID feature)
{
    switch (feature)
    {
        // C# preview features.
        case MessageID.IDS_FeatureUnsafeEvolution:
            return LanguageVersion.Preview;

        // C# 15.0 features.
        case MessageID.IDS_FeatureCollectionExpressionArguments:
        case MessageID.IDS_FeatureUnions:
        case MessageID.IDS_FeatureStaticMembersInInterfaces:
        case MessageID.IDS_FeatureClosedClasses:
        case MessageID.IDS_FeatureLabeledBreakContinue:
        case MessageID.IDS_FeatureExtensionIndexers:
            return LanguageVersion.CSharp15;
        ...
```

`IDS_FeatureUnsafeEvolution = MessageBase + 12859`; its localized string is
`"updated memory safety rules"`.

`src/Compilers/CSharp/Portable/LanguageVersion.cs` — the doc comment on `CSharp15 = 1500`
lists the C# 15 features and **does not include unsafe evolution**:

> Collection expression arguments / Unions / Non-virtual static members in interfaces /
> Closed class hierarchies / Labeled `break` and `continue` / Extension indexers

`LanguageVersionFacts.CurrentVersion => LanguageVersion.CSharp15`,
`CSharpNext = LanguageVersion.Preview`, `MapSpecifiedToEffectiveVersion(LatestMajor) => CSharp15`.
The string parser accepts `"15"`/`"15.0"` as the highest numeric value; `"16"` does not parse.

### 2.2 Roslyn Language Feature Status table

| Feature | Branch | Status |
|---|---|---|
| Unsafe evolution | `features/UnsafeEvolution` | **"Merged as preview feature into .NET 11p2 and VS 18.6"** |
| Unions | `features/Unions` | "C# 15" |
| Closed class hierarchies | `features/closed-class` | "C# 15" |

The wording difference is deliberate: Unions and closed hierarchies are C# 15; unsafe
evolution is a preview feature only.

### 2.3 The "C# 16" wording in the Roslyn breaking-changes document

The file `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md` labels the unsafe
evolution breaks as *"In C# 16"* / *"under `langversion:16`"*, while labelling `closed` and
`union` breaks as *"In C# 15"*. Since no `CSharp16` enum member exists, "C# 16" there means
`LanguageVersion.Preview` (the next version after 15). This is consistent with every other
source: unsafe evolution does not take effect at C# 15.

LDM 2026-08-05 ("Unsafe v2") confirms the direction:

> "The evolution of `unsafe` is already well underway and available in preview. […] We
> therefore expect this to remain a significant focus in C# 16."
> Conclusion: "For C# 16, we will continue the `unsafe` evolution […]"

### 2.4 What Microsoft Learn says (2026-08-19)

`learn.microsoft.com/.../csharp/whats-new/csharp-15` lists "Memory safety" as one of six
C# 15 items, but the body text scopes it precisely:

> "The first step includes the pointer relaxations. **When you compile with the `preview`
> language version**, the following operations no longer require an `unsafe` context […]"

> "Like the rest of the memory safety preview, `unsafe` expressions require the `preview`
> language version and the `AllowUnsafeBlocks` compiler option."

> "The compiler also recognizes the `safe` contextual keyword as a modifier on `extern`
> members and explicit-layout fields. However, the *requires-unsafe* member model and the
> assembly opt-in to the updated memory safety rules aren't available yet, so `safe` and
> `unsafe` currently have no effect on callers."

The last sentence is about what is available through *supported* project properties: there is
no public opt-in property in .NET 11, so from a project file you cannot turn on the
requires-unsafe model except through the undocumented `Features` escape hatch. The Roslyn
implementation of the requires-unsafe model does exist and is reachable via
`CSharpCompilationOptions.WithMemorySafetyRulesVersion(...)` or
`/features:updated-memory-safety-rules`.

`learn.microsoft.com/.../csharp/language-reference/unsafe-code`, section
"The updated memory safety model (preview)":

> "The updated memory safety model is a preview feature in C# 15 and .NET 11. […] The
> compiler currently implements the pointer relaxations and `unsafe` expressions, and it
> recognizes the `safe` keyword. It doesn't yet enforce caller-unsafe obligations or the
> assembly opt-in: there's no public opt-in property yet, so `unsafe` and `safe` have no
> effect on callers."

### 2.5 Ship plan

`dotnet/designs/accepted/2025/memory-safety/sdk-memory-safety-enforcement.md`:

> "In .NET 11, the feature will be off-by-default and in preview, and we will not be
> recommending broad adoption by arbitrary users […] Users will be able to opt-in to the
> feature preview by putting `<Features>$(Features);updated-memory-safety-rules</Features>`
> and `<LangVersion>preview</LangVersion>` into their project files."

> "In the .NET 12 or 13 timeframe […] we will make the feature available via
> `<MemorySafetyRules>2</MemorySafetyRules>`, first as an opt-in. Our aspiration is to
> enable the new memory safety rules by default with opt-out eventually."

> "For .NET 11, we expect that this flag will also need `LangVersion=preview`, as we do not
> expect that the feature will be ready for broad, unconditional adoption until .NET 12 or 13."

File-based programs: opt in with `#:property Features=$(Features);updated-memory-safety-rules`
and `#:property LangVersion=preview`.

The .NET blog post (2026-05-21) says the model "will initially be opt-in" and is planned as a
"preview in .NET 11 and as a production release in .NET 12".

## 3. Exact list of operations that no longer require an `unsafe` context

Source: speclet §"Existing `unsafe` rules" and §"Redefining expressions that require unsafe
contexts"; docs table in `unsafe-code.md`; Roslyn `Binder_Unsafe.GetUnsafeDiagnosticInfo`
(sites called with `disallowedUnder: MemorySafetyRulesVersion.Version1`).

**Relaxed (allowed in safe code under `LangVersion=preview`, regardless of assembly opt-in):**

1. **Pointer type declaration** — `int*`, `int**`, `void*`, `int*[]`, `delegate*<...>` in any
   position (locals, fields, parameters, return types, type arguments where legal).
   C# spec §24.3 "Pointer types" moves into §8 "Types".
2. **Address-of operator `&`** on a variable (`int* p = &i;`), including `&method` producing a
   function pointer (§24.6.5).
3. **Pointer conversions** (§24.5) — implicit to `void*`, explicit pointer↔pointer,
   pointer↔integral. Move into §10 "Conversions".
4. **All other pointer expressions** (§24.6) except the three listed in §4 below:
   pointer arithmetic (`p + n`, `p - n`, `p - q`), `++`/`--` on pointers, pointer comparison
   (`==`, `!=`, `<`, `>`, `<=`, `>=`).
5. **The `fixed` statement** (§24.7) — pinning is not itself a memory access. Moves to §13
   "Statements".
6. **Fixed and moveable variables** (§24.4).
7. **Declaring a fixed-size buffer** (`fixed char name[30];`, §24.8.2). Moves to §16.3
   "Struct members". *Reading* a fixed-size buffer field is also safe; only `element_access`
   on it is unsafe (see §4).
8. **`stackalloc` converted to a pointer** — *always* safe now, in every context.
   Speclet note: "This means that assigning a `stackalloc` to a pointer is _always_ safe,
   regardless of context."
9. **`sizeof` on any unmanaged type.** Previously §24.6.9 required unsafe for non-predefined
   types; `sizeof` on predefined types was already constant and safe (§12.8.19). Now safe for
   all unmanaged types. The speclet says this is "now safe **regardless of opt-in** to the
   updated memory safety rules".
10. **`await` inside an `unsafe` context** (new relaxation; see §7).

**Not relaxed:** a `stackalloc` of a managed type remains an error. C# 11's warning for
pointers to managed types is unchanged (open question whether to relax it for address-of).

## 4. Exact list of operations that still require an `unsafe` context

Source: speclet §"Redefining expressions that require unsafe contexts".

1. **Pointer indirection** — `*p` (§24.6.2).
2. **Pointer member access** — `p->member` (§24.6.3).
3. **Pointer element access** — `p[i]` (§24.6.4).
4. **Function pointer invocation** — calling through a `delegate*<...>` value.
   (Declaring the function pointer type, and `&method` to produce one, are safe.)
5. **Element access on a fixed-size buffer** — when the fixed-size buffer field is the
   *primary_expression* of an `element_access`; this is evaluated as a
   *pointer_element_access*.
6. **`stackalloc` under the tightened rule** (opt-in only — see §5).
7. **Any expression or statement that uses a *requires-unsafe* member** (opt-in / compat mode
   — see §6). This includes indirect uses: `foreach` (`GetEnumerator`/`Current`/`MoveNext`),
   `using` (`Dispose`), deconstruction (`Deconstruct`), `lock`, interpolated string handlers,
   interceptors, patterns, object initializers, `with` expressions, operators and extension
   operators, attribute application, object creation and `new()`-constraint satisfaction.
   **Exception:** `nameof(...)` does *not* report requires-unsafe errors (added in .NET 11
   Preview 7, roslyn PR #84325). `Binder.ReportDiagnosticsIfUnsafeMemberAccess` begins with
   `if (IsInsideNameof) { return; }`.

## 5. The tightened `stackalloc` rule (the only tightening; opt-in only)

Speclet §"Stack allocation". A *stackalloc_expression* is unsafe if **all** hold:

* it is being converted to `Span<T>` or `ReadOnlySpan<T>`;
* it has no *stackalloc_initializer*;
* it occurs within a member that has `SkipLocalsInitAttribute` applied.

Rationale: the stack space has unknown contents and is being wrapped in a type that promises
safe access. "Unlike other changes to `unsafe` rules which are relaxations, this is a
tightening, and hence it applies only under opt-in to the updated memory safety rules to
avoid a break."

Roslyn implementation (`Binder_Unsafe.ReportUnsafeForUninitializedSpanStackAllocIfRequired`):

```csharp
if (!hasInitializer &&
    Compilation.SourceModule.UseUpdatedMemorySafetyRules &&
    ContainingMemberOrLambda is MethodSymbol { AreLocalsZeroed: false })
{
    ReportUnsafeIfNotAllowed(node, diagnostics,
        disallowedUnder: MemorySafetyRulesVersion.Version2,
        customErrorCode: ErrorCode.ERR_UnsafeUninitializedStackAlloc);
}
```

Diagnostic **CS9361** `ERR_UnsafeUninitializedStackAlloc`:
"stackalloc expression without an initializer inside SkipLocalsInit may only be used in an
unsafe context".

The rule was made opt-in in .NET 11 Preview 5 (roslyn PR #83639). LDM has not yet formally
confirmed it (speclet open question "`stackalloc` rule"; roslyn issue #82546 tracks it).

Also new: `stackalloc` pointers do not survive an `await`. Before this feature, pointers were
disallowed in `async` methods so this was not observable.

## 6. The *requires-unsafe* member model

### 6.1 Terminology

Speclet: a member is ***requires-unsafe*** (previously "caller-unsafe") if
- under the **updated** memory safety rules it is marked `unsafe`; or
- under the **legacy** memory safety rules it contains pointers in its signature (compat mode).

Roslyn models this with `internal enum CallerUnsafeMode` in
`src/Compilers/CSharp/Portable/Symbols/CallerUnsafeMode.cs`:

```csharp
internal enum CallerUnsafeMode
{
    /// <summary>The member is not considered caller-unsafe.</summary>
    None,
    /// <summary>The member is implicitly considered caller-unsafe because it contains pointers in its signature.
    /// This state is valid even under the legacy memory safety rules to avoid a dip caused by pointers being safe
    /// regardless of memory safety rules.</summary>
    Implicit,
    /// <summary>The member is explicitly marked as unsafe under the updated memory safety rules.</summary>
    Explicit,
}
```

### 6.2 What changes about `unsafe` on a member (opt-in only)

* `unsafe` on a member marks it *requires-unsafe*: every call site must be in an `unsafe`
  context, and overrides cannot add `unsafe` to a safe base member.
* `unsafe` on a member or type **no longer introduces an `unsafe` context** in the body or
  initializers. Explicit `unsafe` blocks / `unsafe(...)` expressions are required inside.
  (LDM 2026-04-22: "yes, `unsafe` in signature doesn't make the body `unsafe`".)
* `unsafe` is an **error** (`ERR_UnsafeMeaningless`, CS9377) on:
  - type declarations (`class`, `struct`, `interface`, `record`, `enum`, …),
  - static constructors,
  - destructors/finalizers,
  - `delegate` declarations.

  Roslyn gates this on `UseUpdatedMemorySafetyRules`:
  ```csharp
  if ((mods & DeclarationModifiers.Unsafe) == DeclarationModifiers.Unsafe &&
      this.ContainingModule.UseUpdatedMemorySafetyRules)
  {
      diagnostics.Add(ErrorCode.ERR_UnsafeMeaningless, GetFirstLocation());
  }
  ```
* `unsafe` on a **constructor** does introduce an `unsafe` context inside its *initializer*,
  so an `unsafe` constructor may call a *requires-unsafe* `base`/`this` constructor.
* `unsafe`/`safe` on a member is **not** inherited by nested lambdas or local functions.
  Lambdas cannot be marked `unsafe` at all (compile-time error). A local function must be
  marked `unsafe` explicitly to become *requires-unsafe*; a local function inside an `unsafe`
  block is in an unsafe context but is not *requires-unsafe*.
* `partial` members: both parts must agree on `unsafe`, and on `safe`
  (`ERR_PartialMemberUnsafeDifference` CS0764; `ERR_PartialMemberSafeDifference` CS9390).
* **Properties/indexers**: `get` and `set`/`init` accessors can carry `unsafe`/`safe`
  independently; without a modifier they inherit the property's. Restrictions mirror
  `readonly`:
  - not on both the property and its accessors (`ERR_InvalidPropertyUnsafeMods`, CS9396);
  - not the same modifier on *all* accessors (`ERR_SamePropertyUnsafeAccessorMods`, CS9397) —
    put it on the property instead.
* **Events**: `add`/`remove` accessors cannot carry modifiers; only the whole event can be
  `unsafe`.
* **Fields**: `unsafe` on a field makes it *requires-unsafe* (every read/write needs an
  unsafe context) and does not make its initializer an unsafe context.
  Marking a property or field-like event `unsafe` does **not** make its backing field
  *requires-unsafe*. (LDM 2026-05-13.)
* **`new()` / `struct` constraint**: a type whose parameterless constructor is
  *requires-unsafe* does not satisfy `new()` in *declaration* positions at all, and satisfies
  it in *expression* positions only inside an `unsafe` context
  (`ERR_UnsafeConstructorConstraint`, CS9376). Speclet example:

  ```csharp
  class Unsafe { public unsafe Unsafe() { } }
  class C<T> where T : new();
  class D : C<Unsafe> // error: Unsafe cannot satisfy new() in this declaration position
  {
      void M()
      {
          _ = new C<Unsafe>();          // error
          unsafe { _ = new C<Unsafe>(); } // ok
      }
  }
  ```
* **Delegates and lambdas**: converting a *requires-unsafe* member to a delegate type outside
  an `unsafe` context is an error. Delegate types and lambda *function types* cannot be
  *requires-unsafe*.
* **Overriding / implementing**: adding `unsafe` in an override or interface implementation of
  a safe member is an error (`ERR_CallerUnsafeOverridingSafe` CS9364,
  `ERR_CallerUnsafeImplicitlyImplementingSafe` CS9365,
  `ERR_CallerUnsafeExplicitlyImplementingSafe` CS9366).

### 6.3 `extern`

Speclet §"`extern`":

> "Because `extern` methods are to native locations that cannot be guaranteed by the runtime,
> the compiler cannot tell whether they are safe or unsafe. Even methods that only take
> `unmanaged` parameters by value cannot be safely called by C#, as the calling convention
> used for the method could be incorrectly specified by the user and must be manually
> verified by review."

Under the updated rules, **every `extern` member must be explicitly `unsafe` or `safe`**
(`ERR_ExternMemberRequiresUnsafeOrSafe`, CS9389: "'extern' member must be marked 'unsafe' or
'safe'."). Enforced in `SourceMemberMethodSymbol`, `SourceEventSymbol`,
`SourcePropertySymbolBase`, `LocalFunctionSymbol`.

`extern` is the only place where `RequiresUnsafeAttribute` is synthesized without an explicit
`unsafe` keyword. `extern` members coming from *legacy-rules* assemblies are **not** treated
as implicitly `unsafe`, because `extern` is an implementation detail not guaranteed to be
preserved in reference assemblies.

Docs example:

```csharp
[LibraryImport("libc")]
internal static safe partial int getpid();

[LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
internal static unsafe partial nint strlen(byte* str);
```

### 6.4 Explicit / extended layout fields

In a type with `[StructLayout(LayoutKind.Explicit)]` or `[ExtendedLayout]`, **all instance
fields must be marked `safe` or `unsafe`** (`ERR_ExplicitOrExtendedLayoutFieldRequiresUnsafeOrSafe`,
CS9392, reported from `SourceNamedTypeSymbol`). If the field is hidden behind an auto-property
or a field-like event, the requirement moves to that member.

### 6.5 The `safe` contextual keyword

* `SyntaxKind.SafeKeyword = 8454`, contextual (`SyntaxFacts.IsContextualKeyword` returns true;
  `GetContextualKeywordKind("safe") == SafeKeyword`; `GetText(SafeKeyword) == "safe"`).
  It is currently the highest-numbered contextual keyword, so
  `SyntaxFacts.GetContextualKeywordKinds()` iterates `YieldKeyword .. SafeKeyword`.
* `DeclarationModifiers.Safe = 1 << 26` (`DeclarationModifiers.Unsafe = 1 << 15`).
* Requires `IDS_FeatureUnsafeEvolution`, i.e. `LangVersion=preview`
  (`ModifierUtils.cs`; `LanguageParser` calls
  `parseAsModifier(MessageID.IDS_FeatureUnsafeEvolution, out modTok)` for
  `DeclarationModifiers.Safe`).
* `safe` + `unsafe` together is an error: `ERR_SafeModifierCannotBeUsedWithUnsafe` (CS9388),
  "The 'safe' and 'unsafe' modifiers cannot be used together."
* **LDM 2026-07-22 decision**: `safe` is permitted as a declaration modifier **anywhere**
  `unsafe` can mark a declaration *requires-unsafe*; where it is not required, it is a no-op.
  This was driven by `LibraryImport` source generation (roslyn issue #84555): whether the
  generated partial implementation is `extern` is an implementation detail of the generator,
  so the user-authored partial declaration must be able to carry `safe` regardless. Cases
  that need an explicit modifier where the language does not require one will need an analyzer.
* `safe` **only** marks the declaration as not-*requires-unsafe*. It does **not** introduce a
  "safe context". There is no `safe` block and no `safe` expression form.
* On a local function, `safe` says calling it needs no unsafe context; it does not make the
  body a safe context, and a local function declared inside an `unsafe` context stays in it
  (LDM 2026-07-22).
* Spelling is still provisional: LDM 2026-04-13 called it "a temporary spelling"; LDM
  2026-05-13 reaffirmed `safe` but noted it is "still open to revisiting".

**Breaking change**: `safe` used as a type name in a member declaration position now parses as
a modifier. Workaround `@safe`. (Roslyn breaking changes doc, "Introduced in Visual Studio 2026
version 18.9".)

```csharp
class safe { }

class C
{
    safe M1() => new safe();  // previously 'safe' refers to a type, now it is a keyword
    @safe M2() => new safe(); // workaround
}
```

## 7. `await` in `unsafe` contexts, and `await` in `fixed`

Speclet §"`await` in `unsafe` contexts" (answered by LDM 2026-07-01 "allow `await` in `unsafe`"):

* `await` expressions are now **allowed** in `unsafe` contexts.
* `await` remains **disallowed inside a `fixed` statement** — new error
  `ERR_BadAwaitInFixed` (CS9398), "Cannot await in context of a 'fixed' statement".
* Existing `ERR_AwaitInUnsafeContext = 4004` is commented out in `ErrorCode.cs`
  ("replaced with a langversion error").
* Standard text change (§15.15.1 Async functions > General):
  > It is a compile-time error for an unsafe context to contain ~~an `await` expression or~~ a
  > `yield return` statement.
  >
  > **It is a compile-time error for a `fixed` statement to contain an `await` expression.**
* The compiler does not attempt to prove that a pointer kept live across an `await` is still
  valid after resumption; that remains the `unsafe` author's obligation.
* `yield` in `unsafe` contexts, and pointer parameters in async/iterator methods, remain
  open questions.

## 8. `unsafe(expression)` — exact grammar and Roslyn shape

### 8.1 Grammar

Speclet §"`unsafe` expressions". An `unsafe_expression` is added as a
*primary_no_array_creation_expression*:

```antlr
unsafe_expression
    : 'unsafe' '(' expression ')'
    ;
```

Semantics: it establishes an `unsafe` context for evaluating its *expression*. Pointer
dereferences, function pointer invocations and calls to *requires-unsafe* members are all
permitted inside. The type and value of the `unsafe_expression` are those of the enclosed
expression. **The unsafe context does not extend beyond the closing parenthesis.**

Answered by [LDM 2026-05-27 §"unsafe expressions"](https://github.com/dotnet/csharplang/blob/main/meetings/2026/LDM-2026-05-27.md#unsafe-expressions).

### 8.2 Where it may be used

Anywhere a primary expression may appear. Its motivating positions are those where an
`unsafe` *block* cannot appear syntactically:

* **field initializers** — `static int _value = unsafe(ReadFromPointer());`
* **constructor initializers** — `C() : this(unsafe(GetUnsafeValue())) { }`,
  `Derived() : base(unsafe(GetUnsafeValue())) { }`
* **catch filters** — `catch (Exception e) when (unsafe(NowUnsafeCall(e)))`
* **around a single operand of `await`** — `await unsafe(DoWork());` keeps the `await` itself
  outside the unsafe context
* **inline call narrowing** — `Console.WriteLine(unsafe(Add(1, 2)));` keeps `WriteLine` out of
  the unsafe context
* per the Roslyn breaking-changes doc, as the fix for the new compat-mode error:
  `int b = unsafe(c[null]);`

### 8.3 Requirements

* `AllowUnsafeBlocks` (`CSharpCompilationOptions.AllowUnsafe`) must be true, else
  `ERR_IllegalUnsafe` (CS0227).
* `LangVersion=preview` (`MessageID.IDS_FeatureUnsafeEvolution`).
* Availability is **not** conditional on the assembly opt-in: "This new syntax is available
  under new LangVersion, but regardless of opt-in, under the premise that we are trying to
  make it so that anything you are required to do when you are opted in, you are allowed to do
  before you opt in."

Roslyn `Binder_Expressions.BindUnsafeExpression`:

```csharp
private BoundExpression BindUnsafeExpression(UnsafeExpressionSyntax node, BindingDiagnosticBag diagnostics)
{
    var binder = this.GetBinder(node);

    if (!this.Compilation.Options.AllowUnsafe)
    {
        Error(diagnostics, ErrorCode.ERR_IllegalUnsafe, node.Keyword);
    }

    CheckFeatureAvailability(node.Keyword, MessageID.IDS_FeatureUnsafeEvolution, diagnostics);

    return binder.BindParenthesizedExpression(node.Expression, diagnostics);
}
```

### 8.4 Roslyn syntax node (Syntax.xml)

```xml
<Node Name="UnsafeExpressionSyntax" Base="ExpressionSyntax"
      ExperimentalUrl="https://github.com/dotnet/roslyn/issues/82789">
  <Kind Name="UnsafeExpression"/>
  <Field Name="Keyword" Type="SyntaxToken">
    <Kind Name="UnsafeKeyword"/>
  </Field>
  <Field Name="OpenParenToken" Type="SyntaxToken">
    <Kind Name="OpenParenToken"/>
  </Field>
  <Field Name="Expression" Type="ExpressionSyntax"/>
  <Field Name="CloseParenToken" Type="SyntaxToken">
    <Kind Name="CloseParenToken"/>
  </Field>
</Node>
```

**`UnsafeExpressionSyntax` is the only node in the entire Syntax.xml carrying an
`ExperimentalUrl` attribute**, i.e. the only syntax node whose generated public API is marked
`[Experimental]`.

Parser: `LanguageParser.ParseUnsafeExpression()` is reached from the term parser's
`case SyntaxKind.UnsafeKeyword:`; it eats `unsafe`, `(`, calls
`ParseExpressionForParenthesizedConstruct()`, then eats `)`:

```csharp
private UnsafeExpressionSyntax ParseUnsafeExpression()
{
    return _syntaxFactory.UnsafeExpression(
        this.EatToken(SyntaxKind.UnsafeKeyword),
        this.EatToken(SyntaxKind.OpenParenToken),
        this.ParseExpressionForParenthesizedConstruct(),
        this.EatToken(SyntaxKind.CloseParenToken));
}
```

It is **not** langversion-gated in the parser — the tree shape is produced regardless of
LangVersion and the diagnostic is reported during binding. Precedence:
`SyntaxKind.UnsafeExpression => Precedence.Unary` (same as `CheckedExpression`).
`SyntaxKindFacts` maps `SyntaxKind.UnsafeKeyword -> SyntaxKind.UnsafeExpression`.

### 8.5 Binder / semantic model shape (important for tools)

* `LocalBinderFactory.VisitUnsafeExpression` creates
  `_enclosing.WithAdditionalFlags(BinderFlags.UnsafeRegion)` and maps it to the node —
  exactly parallel to `CheckedExpression`:
  ```csharp
  public override void VisitUnsafeExpression(UnsafeExpressionSyntax node)
  {
      Binder binder = _enclosing.WithAdditionalFlags(BinderFlags.UnsafeRegion);
      AddToMap(node, binder);
      Visit(node.Expression, binder);
  }
  ```
* `SyntaxNodeExtensions.CanHaveAssociatedLocalBinder` returns `true` for
  `SyntaxKind.UnsafeExpression`.
* `MemberSemanticModel.GetEnclosingBinder` special-cases `UnsafeExpressionSyntax`: the
  binder applies only between `OpenParenToken` and `CloseParenToken`.
* `MemberSemanticModel.GetBindableSyntaxNode` unwraps it:
  ```csharp
  case UnsafeExpressionSyntax n:
      node = n.Expression;
      continue;
  ```
  There is **no** `BoundUnsafeExpression` and **no** dedicated `IOperation`. Binding returns
  the bound node of the inner expression (via `BindParenthesizedExpression`). So
  `GetTypeInfo`/`GetSymbolInfo`/`GetOperation` on an `UnsafeExpressionSyntax` behave as on the
  inner expression, exactly like `checked(x)` and `(x)`.

## 9. Metadata: `MemorySafetyRulesAttribute` and `RequiresUnsafeAttribute`

### 9.1 Speclet definitions (note: one namespace differs from the implementation)

```csharp
namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates the language version of the memory safety rules used when the module was compiled.</summary>
    [AttributeUsage(AttributeTargets.Module, Inherited = false)]
    public sealed class MemorySafetyRulesAttribute : Attribute
    {
        public MemorySafetyRulesAttribute(int version) => Version = version;
        public int Version { get; }
    }

    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor,
                    AllowMultiple = false, Inherited = false)]
    public sealed class RequiresUnsafeAttribute : Attribute { }
}
```

### 9.2 What actually shipped in the BCL (dotnet/runtime `main`)

`MemorySafetyRulesAttribute` — **`System.Runtime.CompilerServices`** (matches spec)
(`src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/MemorySafetyRulesAttribute.cs`):

```csharp
namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates the version of the memory safety rules used when the module was compiled.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Module, Inherited = false, AllowMultiple = false)]
    public sealed class MemorySafetyRulesAttribute : Attribute
    {
        public MemorySafetyRulesAttribute(int version) => Version = version;
        public int Version { get; }
    }
}
```

`RequiresUnsafeAttribute` — **`System.Diagnostics.CodeAnalysis`**, *not*
`System.Runtime.CompilerServices` as the speclet text says
(`src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/RequiresUnsafeAttribute.cs`):

```csharp
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Indicates that the specified member requires the caller to be in an unsafe context.</summary>
    [AttributeUsage(
        AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property,
        Inherited = false,
        AllowMultiple = false)]
    public sealed class RequiresUnsafeAttribute : Attribute
    {
        public RequiresUnsafeAttribute() { }
    }
}
```

Roslyn agrees with the implementation, not the speclet
(`src/Compilers/Core/Portable/WellKnownTypes.cs`):

```
System_Runtime_CompilerServices_MemorySafetyRulesAttribute
System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute
...
"System.Runtime.CompilerServices.MemorySafetyRulesAttribute",
"System.Diagnostics.CodeAnalysis.RequiresUnsafeAttribute",
```

Both are present in the `System.Runtime` reference assembly on `main`
(`src/libraries/System.Runtime/ref/System.Runtime.cs`), so both ship in .NET 11.
Both are synthesized by the compiler when missing (standard well-known-member behaviour):
`WellKnownMember.System_Runtime_CompilerServices_MemorySafetyRulesAttribute__ctor` and
`WellKnownMember.System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute__ctor`.

### 9.3 Version value: `2`, not `15`

The speclet leaves the value open ("What should be the 'enabled'/'updated' memory safety rules
version? `2`? `15`? `11`?"). **The implementation chose the private numbering (option 3).**

`src/Compilers/Core/Portable/MemorySafetyRulesVersion.cs`:

```csharp
namespace Microsoft.CodeAnalysis;

/// <summary>
/// Memory safety rules version used by a module. See <see cref="IModuleSymbol.MemorySafetyRulesVersion"/> for more details.
/// </summary>
[Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
public enum MemorySafetyRulesVersion
{
    /// <summary>Legacy rules.</summary>
    [Experimental(...)] Version1 = 1,
    /// <summary>Updated rules introduced with the "unsafe evolution" language feature.</summary>
    [Experimental(...)] Version2 = 2,
}
```

`SourceModuleSymbol` emits `MemorySafetyRulesAttribute((int)MemorySafetyRulesVersion)`, i.e.
`MemorySafetyRulesAttribute(2)`, when the version is not `Version1`:

```csharp
if (MemorySafetyRulesVersion != MemorySafetyRulesVersion.Version1)
{
    var version = ImmutableArray.Create(new TypedConstant(compilation.GetSpecialType(SpecialType.System_Int32),
        TypedConstantKind.Primitive, (int)MemorySafetyRulesVersion));
    AddSynthesizedAttribute(ref attributes, moduleBuilder.TrySynthesizeMemorySafetyRulesAttribute(version));
}
```

Any document (including the speclet's §Metadata prose and older blog posts) that says
"filled in with `15` as the language version" is stale.

### 9.4 Rules around the attributes

* It is an error to apply `MemorySafetyRulesAttribute` or `RequiresUnsafeAttribute`
  **explicitly in source**. For the latter: `ERR_RequiresUnsafeAttributeInSource` (CS9379),
  "Do not use 'RequiresUnsafeAttribute' in source; use the 'unsafe' modifier instead."
  (Under legacy rules it is a warning rather than an error — see test plan item
  "warn in legacy mode (see `RequiresUnsafeAttribute_ReferencedInSource`)".)
* When a non-type member is *requires-unsafe*, the compiler synthesizes
  `RequiresUnsafeAttribute` on it in metadata.
* The compiler **ignores** `RequiresUnsafeAttribute`-marked members read from assemblies that
  use the legacy memory safety rules; compat mode is used there instead.
* Well-known members (e.g. `Array.Length`) are assumed safe by the compiler for simplicity.

## 10. Compat mode and cross-assembly behaviour

Speclet §"Compat mode":

> "For such modules [not updated], a member is considered *requires-unsafe* if it contains a
> pointer or function pointer type somewhere among its parameter types or return type (can be
> nested in a non-pointer type, e.g., `int*[]`)."

Excluded from compat mode:
* pointers in **constraint types** (`where T : I<int*[]>`) — those never required an unsafe
  context at call sites before;
* **substituted generic parameters** (`I<T>.M(T)` with `T = int*[]`) — "there is no type-safe
  way for the target member to use that pointer type for anything anyway";
* `nint` / `System.IntPtr` are **not** treated as pointers (LDM 2026-04-29 declined to extend);
* `extern`/`DllImport` from non-opted-in callees are **not** implicitly requires-unsafe;
* there is **no** blanket warning when an opted-in assembly references a non-opted-in one.

Direction matrix (docs `unsafe-code.md`, "Opt-in and cross-assembly behavior"; matches
`Binder_Unsafe.ReportDiagnosticsIfUnsafeMemberAccess`):

| Caller | Callee | Behaviour |
|---|---|---|
| Updated | Updated | The callee's `unsafe` markers travel through metadata; each call to a *requires-unsafe* member needs an enclosing `unsafe` context. |
| Updated | Legacy (original) | **Compat mode**: any callee member with a pointer type in its signature is treated as *requires-unsafe*. |
| Legacy (original) | Updated | Original pointer rules apply. A *requires-unsafe* member with **no** pointer in its signature becomes callable from safe code, because the legacy caller cannot read the new markers. (LDM 2026-04-29: "no change for opted-in callees".) |
| Legacy | Legacy | Compat mode still applies for pointer-in-signature members (this is the new CS9363). |

The compat-mode requirement applies **even to callers that have not opted in**. Rationale from
the speclet:

> "That should avoid a 'dip' where just updating LangVersion (but not updating memory-safety
> rules version) makes most pointer operations safe (including calling functions with pointers
> in signature that will likely be marked as *requires-unsafe* when opted into the updated
> rules), and hence making code less protected in this migration window."

Roslyn:

```csharp
var useUpdatedMemorySafetyRules = this.Compilation.SourceModule.UseUpdatedMemorySafetyRules;
var callerUnsafeMode = symbol.GetCallerUnsafeMode(this.FieldsBeingBound);
if (!useUpdatedMemorySafetyRules && callerUnsafeMode != CallerUnsafeMode.Implicit)
{
    return;   // a legacy caller only sees Implicit (compat-mode) requires-unsafe members
}
```

Landed in .NET 11 Preview 7 (roslyn PR #83660, fixing roslyn issue #81967).

## 11. Diagnostics added (Roslyn `main`)

| Code | ErrorCode name | Message |
|---|---|---|
| CS9360 | `ERR_UnsafeOperation` | This operation may only be used in an unsafe context |
| CS9361 | `ERR_UnsafeUninitializedStackAlloc` | stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context |
| CS9362 | `ERR_UnsafeMemberOperation` | '{0}' must be used in an unsafe context because it is marked as 'unsafe' |
| CS9363 | `ERR_UnsafeMemberOperationCompat` | '{0}' must be used in an unsafe context because it has pointers in its signature |
| CS9364 | `ERR_CallerUnsafeOverridingSafe` | Unsafe member '{0}' cannot override safe member '{1}' |
| CS9365 | `ERR_CallerUnsafeImplicitlyImplementingSafe` | (implicit interface implementation variant) |
| CS9366 | `ERR_CallerUnsafeExplicitlyImplementingSafe` | (explicit interface implementation variant) |
| CS9376 | `ERR_UnsafeConstructorConstraint` | An unsafe context is required for constructor '{0}' marked as 'unsafe' to satisfy the 'new()' constraint of type parameter '{1}' in '{2}' |
| CS9377 | `ERR_UnsafeMeaningless` | The 'unsafe' modifier does not have any effect here under the current memory safety rules. |
| CS9379 | `ERR_RequiresUnsafeAttributeInSource` | Do not use 'RequiresUnsafeAttribute' in source; use the 'unsafe' modifier instead. |
| CS9388 | `ERR_SafeModifierCannotBeUsedWithUnsafe` | The 'safe' and 'unsafe' modifiers cannot be used together. |
| CS9389 | `ERR_ExternMemberRequiresUnsafeOrSafe` | 'extern' member must be marked 'unsafe' or 'safe'. |
| CS9390 | `ERR_PartialMemberSafeDifference` | (partial parts disagree on `safe`) |
| CS9392 | `ERR_ExplicitOrExtendedLayoutFieldRequiresUnsafeOrSafe` | Field in an explicit or extended layout type must be marked 'unsafe' or 'safe'. |
| CS9396 | `ERR_InvalidPropertyUnsafeMods` | Cannot specify 'unsafe' or 'safe' modifiers on both property or indexer '{0}' and its accessor. Remove one of them. |
| CS9397 | `ERR_SamePropertyUnsafeAccessorMods` | Cannot specify the same 'unsafe' or 'safe' modifier on all accessors of property or indexer '{0}'. Instead, put that modifier on the property itself. |
| CS9398 | `ERR_BadAwaitInFixed` | Cannot await in context of a 'fixed' statement |
| CS9400 | `ERR_BadCompilationOptionValueAccepted` | (invalid `MemorySafetyRulesVersion` value) |

Pre-existing codes still used: `ERR_UnsafeNeeded` (CS0214), `ERR_IllegalUnsafe` (CS0227),
`ERR_SizeofUnsafe` (CS0233), `ERR_PartialMemberUnsafeDifference` (CS0764).
`ERR_AwaitInUnsafeContext` (CS4004) is now commented out — "replaced with a langversion error".
Note also `ERR_FeatureNotAvailableInVersion15 = 9399`.

Opting in without `LangVersion=preview` produces `ERR_CompilationOptionNotAvailable` naming
`MemorySafetyRulesVersion`:

```csharp
if (Options.UseUpdatedMemorySafetyRules && !this.IsFeatureEnabled(MessageID.IDS_FeatureUnsafeEvolution))
{
    builder.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_CompilationOptionNotAvailable,
        nameof(Options.MemorySafetyRulesVersion), (int)Options.MemorySafetyRulesVersion,
        LanguageVersion.ToDisplayString(),
        new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureUnsafeEvolution.RequiredVersion())), Location.None));
}
```

## 12. New Roslyn public API (all `[Experimental("RSEXPERIMENTAL006")]`)

`RoslynExperiments.PreviewLanguageFeatureApi = "RSEXPERIMENTAL006"`
(`src/Compilers/Core/Portable/InternalUtilities/RoslynExperiments.cs`), with
`UrlFormat = "https://github.com/dotnet/roslyn/issues/82789"`.

### Microsoft.CodeAnalysis (language-agnostic)

```
[RSEXPERIMENTAL006] Microsoft.CodeAnalysis.MemorySafetyRulesVersion
[RSEXPERIMENTAL006] Microsoft.CodeAnalysis.MemorySafetyRulesVersion.Version1 = 1
[RSEXPERIMENTAL006] Microsoft.CodeAnalysis.MemorySafetyRulesVersion.Version2 = 2
[RSEXPERIMENTAL006] Microsoft.CodeAnalysis.IModuleSymbol.MemorySafetyRulesVersion.get -> Microsoft.CodeAnalysis.MemorySafetyRulesVersion
[RSEXPERIMENTAL006] Microsoft.CodeAnalysis.ISymbol.RequiresUnsafeContext.get -> bool
```

`ISymbol.RequiresUnsafeContext` documentation (verbatim from `ISymbol.cs`):

> Whether this symbol is considered requires-unsafe, i.e., the symbol requires an `unsafe`
> context at its use site. The value of this property depends on the containing module's
> `IModuleSymbol.MemorySafetyRulesVersion`. Under `MemorySafetyRulesVersion.Version1`, symbols
> with pointers in their signature are considered requires-unsafe. Under
> `MemorySafetyRulesVersion.Version2`, symbols marked `unsafe` are considered requires-unsafe.

**Note:** `RequiresUnsafeContext` is on `ISymbol`, so it exists on every symbol kind, and its
meaning is relative to the *declaring module's* rules version, not the consuming compilation's.

### Microsoft.CodeAnalysis.CSharp

```
[RSEXPERIMENTAL006] CSharpCompilationOptions.MemorySafetyRulesVersion.get -> MemorySafetyRulesVersion
[RSEXPERIMENTAL006] CSharpCompilationOptions.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion version) -> CSharpCompilationOptions!
[RSEXPERIMENTAL006] SyntaxKind.SafeKeyword = 8454
[RSEXPERIMENTAL006] SyntaxKind.UnsafeExpression = 8769
[RSEXPERIMENTAL006] Syntax.UnsafeExpressionSyntax  (+ Keyword/OpenParenToken/Expression/CloseParenToken getters,
                                                     Update, With* methods)
[RSEXPERIMENTAL006] SyntaxFactory.UnsafeExpression(ExpressionSyntax! expression) -> UnsafeExpressionSyntax!
[RSEXPERIMENTAL006] SyntaxFactory.UnsafeExpression(SyntaxToken keyword, SyntaxToken openParenToken,
                                                   ExpressionSyntax! expression, SyntaxToken closeParenToken)
[RSEXPERIMENTAL006] CSharpSyntaxVisitor.VisitUnsafeExpression(UnsafeExpressionSyntax! node) -> void
[RSEXPERIMENTAL006] CSharpSyntaxVisitor<TResult>.VisitUnsafeExpression(UnsafeExpressionSyntax! node) -> TResult?
[RSEXPERIMENTAL006] CSharpSyntaxRewriter.VisitUnsafeExpression(UnsafeExpressionSyntax! node) -> SyntaxNode?
[RSEXPERIMENTAL006] UnsafeExpressionSyntax.Accept(CSharpSyntaxVisitor! visitor) -> void
[RSEXPERIMENTAL006] UnsafeExpressionSyntax.Accept<TResult>(CSharpSyntaxVisitor<TResult>! visitor) -> TResult?
```

`CSharpCompilationOptions.MemorySafetyRulesVersion` defaults to
`MemorySafetyRulesVersion.Version1` in every constructor. `WithUpdatedMemorySafetyRules(bool)`
and `UseUpdatedMemorySafetyRules` are internal helpers.

Notes for tooling: `CSharpCompilationOptions` gained a new (internal) constructor parameter,
and the option participates in `Equals`/`GetHashCode`. It is **not yet serialized** into
deterministic-build compilation options
(`// https://github.com/dotnet/roslyn/issues/82546: serialize MemorySafetyRulesVersion here
when it's no longer experimental`).

## 13. Enabling the feature

### 13.1 Compiler feature flag (the only way in .NET 11)

`src/Compilers/Core/Portable/CommandLine/Feature.cs`:

```csharp
internal const string UpdatedMemorySafetyRules = "updated-memory-safety-rules";
```

`SourceModuleSymbol.MemorySafetyRulesVersion`:

```csharp
return _assemblySymbol.DeclaringCompilation.Options.UseUpdatedMemorySafetyRules ||
       _assemblySymbol.DeclaringCompilation.Feature(Feature.UpdatedMemorySafetyRules) != null
    ? MemorySafetyRulesVersion.Version2
    : MemorySafetyRulesVersion.Version1;
```

So either `CSharpCompilationOptions.WithMemorySafetyRulesVersion(Version2)` (API) or
`/features:updated-memory-safety-rules` (command line, `<Features>` in MSBuild) turns it on.

Project file for .NET 11:

```xml
<PropertyGroup>
  <LangVersion>preview</LangVersion>
  <Features>$(Features);updated-memory-safety-rules</Features>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

Confirmed absent from Roslyn `main`: a `/memorysafetyrules:` switch in
`src/Compilers/CSharp/Portable/CommandLine/CSharpCommandLineParser.cs`, and a
`MemorySafetyRules` property in
`src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets`.

### 13.2 Planned (.NET 12/13)

* `/memorysafetyrules:<number>` csc switch
* `<MemorySafetyRules>2</MemorySafetyRules>` MSBuild property
* possibly `<MemorySafetySeverity>` (or `WarningsNotAsErrors` with a `nullable`-style shorthand)
  to downgrade the errors to warnings during migration (LDM 2026-04-29 approved a "middle"
  opt-in level, "but it's not blocking for preview")
* `#:property MemorySafetyRules=1` opt-out for file-based programs
* Roslyn API tracking for these: https://github.com/dotnet/roslyn/issues/82791

### 13.3 `AllowUnsafeBlocks` interaction

`AllowUnsafeBlocks` is **orthogonal** to the memory safety rules version. It gates every
appearance of the `unsafe` keyword (blocks, expressions, member modifiers) and
`SkipLocalsInitAttribute`. The design intent is that projects can opt into the updated rules
*without* `AllowUnsafeBlocks`, so they get errors for calling `Unsafe.As`, `Marshal.*` etc.
Docs table:

| Opt-in property | `AllowUnsafeBlocks` | Result |
|---|---|---|
| On | Off (default) | Safest: updated model, no unsafe code allowed |
| On | On | Updated model, unsafe code allowed |
| Off | Off | Original model, no pointer types |
| Off | On | Original model, pointer types allowed |

Open questions the speclet still lists: whether `AllowUnsafeBlocks` should be required for
`SkipLocalsInitAttribute` under the updated rules, and whether it should be required for
`safe`.

## 14. Breaking changes

Source: Roslyn `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md` plus the
speclet §"Breaking changes".

### 14.1 Under the updated memory safety rules (opt-in)

* `unsafe` on a member also marks it *requires-unsafe*; overrides cannot be `unsafe` if the
  base member is safe.
* `unsafe` on a member or type no longer introduces an `unsafe` context, so explicit `unsafe`
  blocks must be used around unsafe operations in bodies and initializers.
* `extern` members and explicit-layout fields require an explicit `unsafe`/`safe` keyword.
* `stackalloc` under the `SkipLocalsInit` + span + no-initializer condition requires `unsafe`.
* `unsafe` is an error on type declarations, static constructors and destructors.

### 14.2 Under the new LangVersion (no opt-in needed)

**(a) "Pointer types no longer require an unsafe context"** — *Introduced in VS 2026 version 18.7*.
Overload resolution may now consider candidates it previously excluded:

```csharp
class Program
{
    static void Main()
    {
        M(x => { }); // C# 15: prints "2"; preview ("C# 16"): error CS0121 (ambiguous)
    }
    static void M(F1 f) { Console.WriteLine(1); }
    static void M(F2 f) { Console.WriteLine(2); }
}
unsafe delegate void F1(int* x);
delegate void F2(int x);
```

Mitigation: give the lambda explicit parameter types — `M((int x) => { });`

**(b) "`safe` is a contextual keyword"** — *Introduced in VS 2026 version 18.9*. See §6.5.

**(c) "`unsafe` required for more members"** — *Introduced in VS 2026 version 18.9*
(the compat-mode extension to legacy callers):

```csharp
var c = new C();
int a = c.M(null); // error always
int b = c[null];   // no error in C# 15, reports CS9363 in preview

class C
{
    public unsafe int M(int* x) => 0;
    public unsafe int this[int* x] => 0;
}
```

Fix: `int b = unsafe(c[null]);`

## 15. Documentation conventions the feature recommends

* A new XML documentation tag **`<safety>`** on *requires-unsafe* members, stating the
  contract the caller must satisfy. Not yet formalised; an analyzer could flag a
  *requires-unsafe* member missing one.
* **`// SAFETY:`** comments inside each `unsafe` block, recording why the operation is sound
  (modelled on Rust's convention). LDM 2026-05-27 reached "no concrete decision" on whether
  the compiler should check these.

## 16. Not implemented / still open

From the Roslyn test plan (issue 81207, last updated 2026-05-16) — unchecked items:

* `MemorySafetyRules` attribute: LangVer test coverage.
* `[RequiresUnsafe]` on `partial` members (missing tests).
* `unsafe` on **using alias directives** (missing warning) and on **`using static`** directives
  (missing tests).
* Marking public APIs `[Experimental]` (largely done — see §12).
* Adding the attributes to the BCL (runtime PR #125721) — the attribute *types* now exist in
  `main` and in the reference assembly, but **no BCL member is annotated** and the runtime
  build does not set `updated-memory-safety-rules` anywhere in `Directory.Build.props` or the
  CoreLib project.
* Handling of well-known members unexpectedly marked caller-unsafe.
* Whether reflection APIs should be *requires-unsafe*, or need a runtime check.
* Whether `Activator.CreateInstance<T>()` should be *requires-unsafe*.
* Whether `dynamic` should be *requires-unsafe*.
* **"Should `RequiresUnsafe` attribute and compiler flag be considered experimental in .NET 11?"**
* Public API questions: the meaning of `AllowUnsafe`; an API to indicate caller-unsafe/caller-safe.
* Spec open issues (below).

Speclet open questions with no answer yet:

* **Delegate type `unsafe`ty** — whether delegate types, lambdas and function types can be
  *requires-unsafe*; lambda/method-group conversion to safe delegate types; whether the
  *function type* of a lambda/method group changes. If lambdas can be `unsafe`, a syntax
  change is still needed to allow declaring them so.
* **Local functions / lambda safe contexts** — whether nested functions should inherit the
  enclosing unsafe context (current proposal: they do not become *requires-unsafe*).
* **Pointers to managed types** — whether to relax C# 11's warning for address-of; what about
  `sizeof`.
* **`stackalloc` as initialized** — whether the standard's "always uninitialized" text is a bug.
* **`stackalloc` rule** — LDM has not confirmed it, nor whether it should apply regardless of
  opt-in like the other pointer changes.
* **`AllowUnsafeBlocks` meaning** — see §13.3.
* **Unsafe relaxations gated on LangVersion** — decided *not* conditional on the memory safety
  rules version (LDM 2026-04-06); the speclet still carries "TODO: what about LangVersion?".
  In practice the implementation *does* gate them on LangVersion (see §3).
* **`params` collections** whose element type has a *requires-unsafe* constructor.
* **More meaningless-`unsafe` warnings**, e.g. `[ModuleInitializer] unsafe void M() { }`,
  methods with empty or `extern` bodies.
* **Explicit layout and backing fields** — where to put `safe`/`unsafe` for auto-properties,
  field-like events and primary-constructor parameters in explicit-layout types. `safe` and
  `unsafe` are currently disallowed on a parameter declaration.
* **Synthesized members** — whether compiler-generated but reflection-reachable members (e.g.
  an iterator's `MoveNext`) should carry `[RequiresUnsafe]`.
* **`[Out]` + `[SkipLocalsInit]`** — whether calling such a member is unsafe.
* **Taking the address of an uninitialized variable** — whether to require definite assignment
  before `&`, or to make it unsafe. Examples:
  ```csharp
  static void SkipInit<T>(out T value)
  {
      // value is considered definitely assigned after the address-of
      fixed (void* ptr = &value);
  }

  int i;
  _ = &i;  // i is considered definitely assigned after the address-of
  i++;     // incrementing whatever was on the stack
  ```
* **`new()` constraint with `using` aliases and `using static`** — error at the `using`
  declaration (suppressible with `using unsafe X = ...`) or at the use site. In the latter
  case a "meaningless `unsafe`" warning for using aliases and static usings would be needed.
* **Should more constructs be `unsafe`?** — `dynamic` (should probably match what the BCL
  decides for reflection APIs).
* **Value of `MemorySafetyRulesAttribute`** — spec still open; implementation uses `2`.
* **Suppressing *requires-unsafe* errors in edge cases** — attribute application and other
  non-executable code remain unsuppressible errors (LDM 2026-05-13), pending feedback.

Decided (for the record):

* Errors, not warnings, when the new rules are on (LDM 2025-11-05).
* No source-generator exemption (LDM 2025-11-05).
* Keyword `unsafe`, not an attribute, marks *requires-unsafe* (LDM 2025-11-12; reversed
  2026-01-26 to an attribute; re-reversed 2026-04-06 back to the keyword).
* `unsafe` on a type is an error, not a warning (LDM 2026-05-13), revisitable on feedback.
* No nullable-style region-based (`#pragma`/directive) opt-in (LDM 2026-04-29).
* A "middle" warning-level opt-in: yes in principle, not blocking for preview (LDM 2026-04-29).
* Compat mode applies to non-opted-in callers too (LDM 2026-04-22 / 2026-04-29), severity error.
* Compat mode is *not* extended to `nint`/`IntPtr`, nor to `extern`/`DllImport` from
  non-opted-in assemblies (LDM 2026-04-29).
* Members with `unsafe` blocks or pointers in their signature do **not** need an explicit
  `safe` marker (LDM 2026-04-13).
* `safe` permitted everywhere `unsafe` can mark a declaration *requires-unsafe* (LDM 2026-07-22).
* `await` allowed in `unsafe` (LDM 2026-07-01); `await` in `fixed` disallowed.
* `unsafe` expressions: yes (LDM 2026-05-27).
* Expression trees: resolved (test plan item checked; details not in the speclet).
* No Visual Basic support is needed (no `unsafe` contexts or pointers in VB).

## 17. Quick reference for tooling authors

* New `SyntaxKind` values to handle: `UnsafeExpression = 8769`, `SafeKeyword = 8454`.
  Neighbouring C# 15 additions for context: `UnionKeyword = 8452`, `ClosedKeyword = 8453`,
  `UnionDeclaration = 9082`, `WithElement = 9081`.
* `UnsafeExpressionSyntax` derives from `ExpressionSyntax` and has the same shape as
  `CheckedExpressionSyntax` (keyword, `(`, expression, `)`), with `Precedence.Unary`.
* A `SyntaxTokenList` of modifiers may now contain a `SafeKeyword` token on any member
  declaration; `ToDeclarationModifier(SyntaxKind.SafeKeyword) == DeclarationModifiers.Safe`.
* `unsafe(...)` is a binder scope: `CanHaveAssociatedLocalBinder` is true for it, and
  `GetBindableSyntaxNode` unwraps it to the inner expression. There is no distinct bound node
  or `IOperation`.
* `ISymbol.RequiresUnsafeContext` is the single query for "does calling this need an unsafe
  context", and `IModuleSymbol.MemorySafetyRulesVersion` says which rules the declaring module
  used. Both are `RSEXPERIMENTAL006`.
* Under legacy rules (the default at .NET 11 GA) `ISymbol.RequiresUnsafeContext` is true for
  members with pointers anywhere in the signature — including for symbols read from
  already-shipped assemblies. Callers of pointer-signature members now need an unsafe context
  even without any opt-in (CS9363), which is a real behaviour change under `LangVersion=preview`.
* `SyntaxFacts.GetContextualKeywordKinds()` iterates `YieldKeyword .. SafeKeyword`, so any code
  that hard-codes the upper bound of that range must be updated.
