# C# 15 / .NET 11 — Collection expression arguments, extension indexers, labeled break/continue

Research date: 2026-09-03. All facts verified against primary sources (dotnet/roslyn `main`,
dotnet/csharplang `main`, learn.microsoft.com) on that date. .NET 11 / C# 15 GA is November 2026;
at the time of research .NET 11 is in preview and Visual Studio 2026 is at 18.x.

---

## 0. Cross-cutting facts (apply to all three features)

### 0.1 All three are stable (non-preview) C# 15 features

`src/Compilers/CSharp/Portable/Errors/MessageID.cs` on `main` maps all three feature IDs to
`LanguageVersion.CSharp15`, **not** `LanguageVersion.Preview`:

```csharp
// C# preview features.
case MessageID.IDS_FeatureUnsafeEvolution:
    return LanguageVersion.Preview;

// C# 15.0 features.
case MessageID.IDS_FeatureCollectionExpressionArguments:
case MessageID.IDS_FeatureUnions:
case MessageID.IDS_FeatureStaticMembersInInterfaces:
case MessageID.IDS_FeatureClosedClasses: // semantic check
case MessageID.IDS_FeatureLabeledBreakContinue:
case MessageID.IDS_FeatureExtensionIndexers:
    return LanguageVersion.CSharp15;
```

New MessageID values (`MessageBase + n`):

| MessageID | Value | Localized name |
|---|---|---|
| `IDS_FeatureCollectionExpressionArguments` | `MessageBase + 12858` | "collection expression arguments" |
| `IDS_FeatureExtensionIndexers` | `MessageBase + 12863` | "extension indexers" |
| `IDS_FeatureLabeledBreakContinue` | `MessageBase + 12864` | "labeled break and continue" |

`src/Compilers/CSharp/Portable/LanguageVersion.cs` on `main`:

```csharp
/// <summary>
/// C# language version 15.0
/// Features:
///   Collection expression arguments
///   Unions
///   Non-virtual static members in interfaces
///   Closed class hierarchies
///   Labeled break and continue
///   Extension indexers
/// </summary>
CSharp15 = 1500,
```

- `LanguageVersionFacts.CurrentVersion => LanguageVersion.CSharp15`
- `MapSpecifiedToEffectiveVersion`: `Latest`, `Default`, `LatestMajor` → `CSharp15`; `Preview` maps
  to itself (so `Preview` is strictly greater than `CSharp15`).
- New error code `ERR_FeatureNotAvailableInVersion15 = 9399` (CS9399):
  *"Feature '{0}' is not available in C# 15.0. Please use language version {1} or greater."*
- There is **no** `LanguageVersion.CSharp16` member on `main` as of 2026-09-03. The Roslyn breaking
  changes document nevertheless refers to "C# 16" and `langversion:16` for the unsafe-evolution items
  (VS 18.7 / 18.9); those are the features currently gated behind `LanguageVersion.Preview`. See
  open questions.

Public API surface (from `src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`) —
`LanguageVersion.CSharp15 = 1500` is a new public enum member. None of the new syntax API for these
three features carries an `[RSEXPERIMENTAL…]` marker (unlike the unsafe-evolution API, which is
marked `[RSEXPERIMENTAL006]`). So the new syntax API is plain public API.

### 0.2 Roslyn tracking

| Feature | csharplang champion | Roslyn test plan / merge | Roslyn branch | State (2026-09-03) |
|---|---|---|---|---|
| Collection expression arguments | [csharplang#8887](https://github.com/dotnet/csharplang/issues/8887) | [roslyn#80613](https://github.com/dotnet/roslyn/issues/80613) | `features/collection-expression-arguments` | test plan **closed / completed** (2026-01-21) |
| Extension indexers | [csharplang#9856](https://github.com/dotnet/csharplang/issues/9856) | [roslyn#81505](https://github.com/dotnet/roslyn/issues/81505) | `features/extensions` | test plan **open**, several items unchecked (last update 2026-06-19) |
| Labeled break/continue | [csharplang#9875](https://github.com/dotnet/csharplang/issues/9875) | [roslyn#83209](https://github.com/dotnet/roslyn/issues/83209) test plan; merged by [roslyn PR#84271](https://github.com/dotnet/roslyn/pull/84271) (merged 2026-06-25, commit `cb96af31028870b9647fab2883e8604e910be0b0`) | `features/labeled-break-and-continue` | merged into `main`; test plan open, only "Public API review" ([roslyn#83266](https://github.com/dotnet/roslyn/issues/83266)) unchecked |

Speclets (note the path: they were moved into `proposals/csharp-15.0/` — the bare
`proposals/<name>.md` URLs now 404):

- <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/collection-expression-arguments.md>
- <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/extension-indexers.md>
- <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/labeled-break-continue.md>

Roslyn feature status: <https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>
(the "C# 15.0" table lists exactly: Collection expression arguments, Unions, Non-virtual static
interface members without DIM runtime support, `ExtendedLayoutAttribute`, Closed class hierarchies,
Extension indexers, Labeled break/continue).

Docs: <https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15>

---

## 1. Collection expression arguments — the `with(...)` element

### 1.1 Grammar

Speclet diff against the C# 12 collection-expressions grammar:

```diff
collection_element
   : expression_element
   | spread_element
+  | with_element
   ;

+with_element
+  : 'with' argument_list
+  ;
```

`with` is a **contextual** keyword here (`SyntaxKind.WithKeyword` already existed for `with`
expressions). Ambiguity resolution, quoting the speclet:

> if the element lexically starts with the token sequence `with` `(` then it is always treated as a
> `with_element`.

To call a method actually named `with` from inside a collection expression, escape it: `@with(x, y)`.

### 1.2 Roslyn syntax model

`src/Compilers/CSharp/Portable/Syntax/Syntax.xml` on `main`:

```xml
<AbstractNode Name="CollectionElementSyntax" Base="CSharpSyntaxNode" />
<Node Name="ExpressionElementSyntax" Base="CollectionElementSyntax">
  <Kind Name="ExpressionElement"/>
  <Field Name="Expression" Type="ExpressionSyntax" />
</Node>
<Node Name="SpreadElementSyntax" Base="CollectionElementSyntax">
  <Kind Name="SpreadElement"/>
  <Field Name="OperatorToken" Type="SyntaxToken"><Kind Name="DotDotToken"/></Field>
  <Field Name="Expression" Type="ExpressionSyntax" />
</Node>
<Node Name="WithElementSyntax" Base="CollectionElementSyntax">
  <Kind Name="WithElement"/>
  <Field Name="WithKeyword" Type="SyntaxToken">
    <Kind Name="WithKeyword"/>
  </Field>
  <Field Name="ArgumentList" Type="ArgumentListSyntax" />
</Node>
```

`CollectionExpressionSyntax.Elements` remains
`SeparatedSyntaxList<CollectionElementSyntax>` with `AllowTrailingSeparator="true"`.

New public API (PublicAPI.Unshipped.txt):

```
Microsoft.CodeAnalysis.CSharp.SyntaxKind.WithElement = 9081
Microsoft.CodeAnalysis.CSharp.Syntax.WithElementSyntax
    .WithKeyword.get -> SyntaxToken
    .ArgumentList.get -> ArgumentListSyntax!
    .Update(SyntaxToken withKeyword, ArgumentListSyntax! argumentList) -> WithElementSyntax!
    .WithWithKeyword(SyntaxToken) -> WithElementSyntax!
    .WithArgumentList(ArgumentListSyntax!) -> WithElementSyntax!
    .AddArgumentListArguments(params ArgumentSyntax![]!) -> WithElementSyntax!
static SyntaxFactory.WithElement(ArgumentListSyntax? argumentList = null) -> WithElementSyntax!
static SyntaxFactory.WithElement(SyntaxToken withKeyword, ArgumentListSyntax! argumentList) -> WithElementSyntax!
virtual CSharpSyntaxVisitor.VisitWithElement(WithElementSyntax!) -> void
virtual CSharpSyntaxVisitor<TResult>.VisitWithElement(WithElementSyntax!) -> TResult?
override CSharpSyntaxRewriter.VisitWithElement(WithElementSyntax!) -> SyntaxNode?
```

**This is a brand-new syntax node type.** Anything generated from `Syntax.xml` (Metalama's
`MetaSyntaxRewriter`, `GenerateMetaSyntaxRewriter`) must be regenerated. `SyntaxKind.WithElement`
is `9081`; `UnionDeclaration` is `9082`, so the C# 15 wave consumed 9081–9082 in the node-kind range
and `UnionKeyword = 8452`, `ClosedKeyword = 8453`, `SafeKeyword = 8454` in the keyword range.

### 1.3 Parsing is unconditional (no language-version gate)

`LanguageParser.ParseCollectionElement` on `main`:

```csharp
private CollectionElementSyntax ParseCollectionElement()
{
    // Even though `with(` could start a legal expression (like `with(x) + y`), spec mandates that if we see
    // `with(` at the start of a collection element, we only parse it as a with-element.
    if (this.CurrentToken.ContextualKind == SyntaxKind.WithKeyword &&
        this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
    {
        return _syntaxFactory.WithElement(this.EatContextualToken(SyntaxKind.WithKeyword), this.ParseParenthesizedArgumentList());
    }

    // Like above, even though `..` could start a legal expression (like `..` (a naked-range)), the spec
    // mandates that if we see `..` at the start of a collection element, we only parse it as a spread-element.
    if (this.IsAtDotDotToken())
        return _syntaxFactory.SpreadElement(this.EatDotDotToken(), this.ParseExpressionCore());

    return _syntaxFactory.ExpressionElement(this.ParseExpressionCore());
}
```

There is **no `LanguageVersion` check in the parser**. The tree shape therefore changes for every
`[with( … )]` regardless of `LangVersion`; the feature check is reported during binding.

### 1.4 Binding

`Binder.BindCollectionExpression` (`Binder_Expressions.cs`, ~line 5349):

```csharp
MessageID.IDS_FeatureCollectionExpressions.CheckFeatureAvailability(diagnostics, syntax, syntax.OpenBracketToken.GetLocation());

BoundUnconvertedWithElement? firstWithElement = null;
var builder = ArrayBuilder<BoundNode>.GetInstance(syntax.Elements.Count);
foreach (var element in syntax.Elements)
{
    if (element is WithElementSyntax withElementSyntax)
    {
        MessageID.IDS_FeatureCollectionExpressionArguments.CheckFeatureAvailability(diagnostics, syntax, withElementSyntax.WithKeyword.GetLocation());
        var (withElement, badElement) = bindWithElement(this, syntax, withElementSyntax, diagnostics);
        firstWithElement ??= withElement;
        builder.AddIfNotNull(badElement);
    }
    else
    {
        builder.Add(bindElement(element, diagnostics, this, nestingLevel));
    }
}
return new BoundUnconvertedCollectionExpression(syntax, firstWithElement, builder.ToImmutableAndFree());
```

`bindWithElement`:
- calls `BindArgumentsAndNames(withElementSyntax.ArgumentList, …, allowArglist: true)`;
- rejects any argument whose type is `dynamic` with `ERR_CollectionArgumentsDynamicBinding`;
- if the `with` element is `syntax.Elements.First()`, produces `BoundUnconvertedWithElement`
  (arguments, names, ref kinds);
- otherwise reports `ERR_CollectionArgumentsMustBeFirst` and produces a `BoundBadExpression` so
  the arguments remain in the tree for IDE analysis.

New bound node: `BoundUnconvertedWithElement`. `BoundUnconvertedCollectionExpression` gained a
`WithElement` child.

Nesting guard `MaxNestingLevel = 64` for nested collection expressions is unchanged.

### 1.5 New diagnostics

From `ErrorCode.cs` and `CSharpResources.resx` on `main`:

| Code | ErrorCode | Message |
|---|---|---|
| CS9354 | `ERR_CollectionArgumentsMustBeFirst` | `'with(...)' element must be the first element` |
| CS9355 | `ERR_CollectionArgumentsNotSupportedForType` | `'with(...)' elements are not supported for type '{0}'` |
| CS9356 | `ERR_CollectionArgumentsDynamicBinding` | `'with(...)' element arguments cannot be dynamic` |
| CS9357 | `ERR_CollectionArgumentsMustBeEmpty` | `'with(...)' element for a read-only interface must be empty if present` |
| CS9358 | `ERR_CollectionRefLikeElementType` | `Element type of this collection may not be a ref struct or a type parameter allowing ref structs` |
| CS9359 | `ERR_BadCollectionArgumentsArgCount` | `No overload for method '{0}' takes {1} 'with(...)' element arguments` |

(Note: several Roslyn test files still carry stale comments referencing CS9335/CS9337 from an
earlier numbering; the numbers above come from `ErrorCode.cs` on `main`.)

### 1.6 Where `with(...)` may appear

- Only inside a collection expression `[ … ]`.
- Must be the **first** element (`ERR_CollectionArgumentsMustBeFirst` otherwise).
- Only one is meaningful; later ones are bound for error recovery but flagged.
- Arguments may not be `dynamic` (LDM-2025-01-22).
- `__arglist` is *not* supported in `with()` (LDM-2025-04-14) — though the binder passes
  `allowArglist: true` to `BindArgumentsAndNames`, so this behaviour deserves a re-check.
- Not supported for arrays or `Span<T>`/`ReadOnlySpan<T>`, **not even empty `with()`**
  (LDM-2025-05-12):
  ```csharp
  Span<int> a = [with(), 1, 2, 3]; // error: arguments not supported
  int[]     b = [with(length: 1), 3]; // error: arguments not supported
  ```

### 1.7 Conversions

The C# 12 conversion clause is amended (speclet):

```diff
> A struct or class type that implements System.Collections.IEnumerable where:

-  * The type has an applicable constructor that can be invoked with no arguments, and the constructor is accessible at the location of the collection expression.
+  a. the collection expression has no `with_element` and the type has an applicable constructor
+     that can be invoked with no arguments, accessible at the location of the collection expression. or
+  b. the collection expression has a `with_element` and the type has at least one constructor
+     accessible at the location of the collection expression.
```

Key consequences:
- Only the **presence or absence** of the `with_element` affects convertibility, never the actual
  arguments. Arguments are ignored in conversion and in type inference (LDM-2025-03-17). Example
  from the speclet:
  ```csharp
  Print([with(comparer: null), 1, 2, 3]); // ambiguous, not resolved by the argument
  static void Print<T>(List<T> list) { }
  static void Print<T>(HashSet<T> set) { }
  ```
- A collection type with **no** parameterless constructor is now convertible-from a collection
  expression, but only if that expression carries a `with_element` (LDM-2025-04-14). Such a type
  still cannot be used for a `params` parameter.

### 1.8 Construction and lowering

Speclet, *Construction*:

- Elements are evaluated left to right; within *collection arguments* the arguments are evaluated
  left to right. Each element/argument is evaluated exactly once. Because `with(...)` is first,
  arguments are evaluated **before** the elements — an explicit design goal.
- If `with(...)` is not the first element → compile-time error.
- `dynamic` argument → compile-time error.

**(a) Constructor case.** Target is a `struct`/`class` implementing `System.Collections.IEnumerable`,
no *create method*, not a generic parameter type:
- Standard overload resolution among all accessible instance constructors declared on the target
  type, applicable with respect to the `with(...)` argument list.
- The chosen constructor is invoked with that argument list; `params` may be used in expanded form.
- Then elements are added by the normal C# 12 collection-expression path (`Add` / `AddRange`).

```csharp
// List<T> candidates: List<T>(), List<T>(IEnumerable<T>), List<T>(int capacity)
List<int> l;
l = [with(capacity: 3), 1, 2]; // new List<int>(capacity: 3)
l = [with([1, 2]), 3];         // new List<int>(IEnumerable<int> collection)
l = [with(default)];           // error: ambiguous constructor
```

```csharp
List<string> names = [with(/*capacity*/10), ...];
// lowers to:
__result = new List<string>(10); // followed by normal initialization
```

**(b) `CollectionBuilderAttribute` / create-method case.**
- The *create methods* rule is **updated** relative to C# 12:
  - the method must have a **last** parameter of type `System.ReadOnlySpan<E>`, passed by value
    (previously it had to be the *only* parameter);
  - **multiple** create methods are now supported (previously exactly one);
  - additional parameters may precede the `ReadOnlySpan<E>` parameter;
  - the method must be named per the attribute, declared directly on the builder type, `static`,
    accessible, and its arity must match the collection type's arity; methods on base types or
    interfaces are ignored.
- Overload resolution is done over *projection methods*: for each create method, a hypothetical
  method with the identical signature **minus the last parameter**. Whichever projection method wins
  against the `with(...)` argument list selects the corresponding create method.
- The create method is invoked with `with(...)` arguments followed by a `ReadOnlySpan<T>` of the
  elements.

```csharp
[CollectionBuilder(typeof(MyBuilder), "Create")]
class MyCollection<T> { }

class MyBuilder
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> elements);
    public static MyCollection<T> Create<T>(IEqualityComparer<T> comparer, ReadOnlySpan<T> elements);
}

MyCollection<string> c1 = [with(GetComparer()), "1", "2"];
// IEqualityComparer<string> _tmp1 = GetComparer();
// ReadOnlySpan<string> _tmp2 = ["1", "2"];
// c1 = MyBuilder.Create<string>(_tmp1, _tmp2);

MyCollection<string> c2 = [with(), "1", "2"];
// ReadOnlySpan<string> _tmp3 = ["1", "2"];
// c2 = MyBuilder.Create<string>(_tmp3);
```

LDM-2025-03-12 settled that the span parameter goes **last** (so it can be `params`, allowing
`MySetBuilder.Create(StringComparer.Ordinal, x, y, z)` to be called directly).

**(c) Interface target types.** Candidate signatures per interface (speclet, matching the
learn.microsoft.com table):

| Interfaces | Candidate signatures |
|---|---|
| `IEnumerable<E>`, `IReadOnlyCollection<E>`, `IReadOnlyList<E>` | `()` (no parameters — same meaning as no `with()`) |
| `ICollection<E>`, `IList<E>` | `List<E>()`, `List<E>(int)` |

For `IList<T>`/`ICollection<T>` the compiler constructs a `List<T>` with the selected constructor.
`ERR_CollectionArgumentsMustBeEmpty` covers a non-empty `with()` on a read-only interface.

**(d) Dictionary interfaces** (`IDictionary<K,V>` → `Dictionary<K,V>()/(int)/(IEqualityComparer<K>)/(int, IEqualityComparer<K>)`;
`IReadOnlyDictionary<K,V>` → `()` and `(IEqualityComparer<K>? comparer)`) are specified in
`collection-expression-arguments.md` but belong to the **dictionary expressions** feature, which is
still "In progress" in the Roslyn working set ([roslyn#81860](https://github.com/dotnet/roslyn/issues/81860)),
i.e. **not** in C# 15 GA. The learn.microsoft.com interface table for C# 15 lists only the five
non-dictionary interfaces.

**(e) All other target types** (arrays, spans, type parameters): binding error for the argument
list, even when empty.

### 1.9 Ref safety

- *Create methods*: the collection expression's safe-context is the safe-context of an invocation of
  the create method where the arguments are the `with()` arguments followed by the collection
  expression itself as the last (`ReadOnlySpan<E>`) argument. The *method arguments must match*
  constraint is applied the same way.
- *Constructor calls*, for `[with(a₁ … aₙ), e₁ … eₙ]` of a `ref struct` type: safe-context is the
  narrowest of the safe-context of `new C(a₁ … aₙ)` and the safe-contexts of the element expressions
  (or spread values). *Method arguments must match* is applied by treating the whole thing as
  `new C(a₁ … aₙ) { e₁ … eₙ }`, with expression elements treated as collection element initializers
  and spread elements treated as if `C` had `Add(SpreadType spread)`.

### 1.10 IOperation changes (important, and not limited to `with()`)

From `Microsoft.CodeAnalysis` `PublicAPI.Unshipped.txt` and `OperationInterfaces.xml`:

```
Microsoft.CodeAnalysis.OperationKind.CollectionExpressionElementsPlaceholder = 129
Microsoft.CodeAnalysis.Operations.ICollectionExpressionOperation.ConstructArguments.get -> ImmutableArray<IOperation!>
Microsoft.CodeAnalysis.Operations.ICollectionExpressionElementsPlaceholderOperation
virtual OperationVisitor.VisitCollectionExpressionElementsPlaceholder(ICollectionExpressionElementsPlaceholderOperation!) -> void
virtual OperationVisitor<TArgument, TResult>.VisitCollectionExpressionElementsPlaceholder(ICollectionExpressionElementsPlaceholderOperation!, TArgument) -> TResult?
```

`ICollectionExpressionOperation` node definition on `main`:

```xml
<Node Name="ICollectionExpressionOperation" Base="IOperation" HasType="true"
      ChildrenOrder="ConstructArguments,Elements">
```

The `ChildrenOrder` **changed** — `ConstructArguments` now precedes `Elements`, so
`IOperation.ChildOperations` enumerates differently than in C# 12–14.

`ConstructMethod` documentation was rewritten:

> Method used to construct the collection.
> 1. If the collection type is an array, span, or type parameter, the method is null.
> 2. If the collection type has a `[CollectionBuilder]` attribute, the method is the builder method.
> 3. If the collection type is a mutable array interface and the collection was initialized with
>    arguments, the method is the constructor of `List{T}` that was used. If this is read-only array
>    interface, or no arguments were provided, the method is null.
> 4. Otherwise, the method is the collection type constructor.

`ConstructArguments` documentation:

> Arguments passed to `ConstructMethod`, if present. Arguments are in evaluation order. This can be
> an empty array. Will never be `default`. If the arguments successfully bound, these will all be
> `IArgumentOperation`; otherwise, they can be any operation.
> …
> If the invocation is in its expanded form, then params/ParamArray arguments would be collected into
> arrays. Default values are supplied for optional arguments missing in source.
> If this is a collection builder method, this will include all arguments to the method, except for
> the final `ReadOnlySpan` argument that receives the collection elements. That final argument will
> be represented by an `IArgumentOperation` whose 'Value' is an
> `ICollectionExpressionElementsPlaceholderOperation`. The actual elements passed to the creation
> method are contained in `Elements`.

`ICollectionExpressionElementsPlaceholderOperation`:

> Represents the elements of a collection expression as they are passed to some construction method
> specified by a `[CollectionBuilder]` attribute. This is distinct from
> `ICollectionExpressionOperation.Elements` which contains the elements as they appear in source.
> This will appear as the 'Value' of an `IArgumentOperation` in
> `ICollectionExpressionOperation.ConstructArguments` when the construction method is a collection
> builder method, representing the final `ReadOnlySpan` passed to that construction method containing
> the fully evaluated elements of the collection expression.

### 1.11 SemanticModel

`CSharpSemanticModel` gained:

```csharp
/// <summary>
/// Returns what symbol(s), if any, the given 'with(...)' element syntax bound to in the program.
/// </summary>
internal SymbolInfo GetSymbolInfo(WithElementSyntax withElement, CancellationToken cancellationToken = default)
```

It is `internal`, but `GetSymbolInfoFromNode` (used by the public `GetSymbolInfo(SyntaxNode, …)`
overload) dispatches to it:

```csharp
case WithElementSyntax withElement:
    return this.GetSymbolInfo(withElement, cancellationToken);
```

`WithElementSyntax` was also added to the `CanGetSemanticInfo` allow-list alongside
`ConstructorInitializerSyntax`, `PrimaryConstructorBaseTypeSyntax`, `AttributeSyntax`, `CrefSyntax`.
So `semanticModel.GetSymbolInfo((SyntaxNode)withElementSyntax)` returns the selected constructor or
create method.

### 1.12 IDE / tooling additions

Files referencing `WithElementSyntax` in the IDE layer on `main`:
- `src/Features/CSharp/Portable/SignatureHelp/WithElementSignatureHelpProvider.cs` (new signature
  help provider)
- `src/Features/CSharp/Portable/Completion/CompletionProviders/NamedParameterCompletionProvider.cs`
  (named-argument completion inside `with(`)
- `src/Workspaces/SharedUtilitiesAndExtensions/Workspace/CSharp/Extensions/WithElementSyntaxExtensions.cs`

Compiler test files: `CollectionExpressionTests_WithElement_ArraysAndSpans.cs`,
`CollectionExpressionTests_WithElement_Constructor.cs`, `CollectionExpressionTests_WithElement_Nullable.cs`
under `src/Compilers/CSharp/Test/Emit3/Semantics/`.

### 1.13 Breaking change

Documented in `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`
(and on learn.microsoft.com), **"Introduced in Visual Studio 2026 version 18.4"**:

> `with(...)` when used as an element in a collection expression, and when the LangVersion is set to
> 15 or greater, is bound as arguments passed to constructor or factory method used to create the
> collection, rather than as an invocation expression of a method named `with`.
> To bind to a method named `with`, use `@with` instead.

```csharp
object x, y, z = ...;
object[] items;

items = [with(x, y), z];  // C# 14: call to with() method; C# 15: error args not supported for object[]
items = [@with(x, y), z]; // call to with() method
object with(object a, object b) { ... }
```

LDM-2025-03-17 resolved: "Keep previous behavior (no breaking change) when compiling with earlier
language version." The speclet still carries this as an **open question** ("Finalizing an open
concern from LDM-2025-03-17"): whether the new parsing applies unconditionally or only at
`LangVersion 15`. As implemented on `main`, the parse is unconditional and the *binder* reports the
language-version diagnostic — see open questions below.

### 1.14 Related-but-separate feature

`with(...)` is the enabling mechanism for **dictionary expressions** (`[k: v, …]`,
[csharplang#8659](https://github.com/dotnet/csharplang/issues/8659)). Dictionary expressions are in
the Roslyn *Working Set* as "In progress" ([roslyn#81860](https://github.com/dotnet/roslyn/issues/81860))
and are **not** part of C# 15.

---

## 2. Extension indexers

### 2.1 Grammar

Diff relative to the C# 14 extensions speclet (`proposals/csharp-14.0/extensions.md`):

```antlr
extension_member_declaration
        : method_declaration
        | property_declaration
        | indexer_declaration // new
        | operator_declaration
        ;
```

So in C# 14 an extension block accepted **methods, properties and operators**; C# 15 adds
**indexers** and nothing else.

Example:

```csharp
public static class BitExtensions
{
    extension(int i)
    {
        public bool this[int index]
        {
            get => ...;
        }
    }
}
```

### 2.2 Declaration rules

- Indexers have no identifier; they are identified by their parameter list, as usual.
- The full set of ordinary-indexer features is available: accessor bodies, expression-bodied
  members, ref-returning accessors, `scoped` parameters, attributes, default parameter values,
  `params`.
- **Because indexers are always instance members, an extension block that declares an indexer must
  provide a named receiver parameter.** (`extension(int)` without a name cannot declare an indexer.)
- Extension members have **no implicit or explicit `this`**; the receiver is reached through the
  named receiver parameter.
- Disallowed modifiers (inherited from the general extension-member rules): `abstract`, `virtual`,
  `override`, `new`, `sealed`, `partial`, `protected` (and the other accessibility modifiers), and
  `init` accessors. The Roslyn test plan also lists `static` as disallowed for extension indexers.
- The extension-member inferrability rule still applies: for each non-method extension member, all
  the type parameters of its extension block must be used in the combined set of parameters from
  the extension and the member.
- `IndexerNameAttribute` may be applied. It is **not emitted** in metadata, but its value affects
  member-conflict checking, determines the metadata name of the property and accessors, and is used
  when emitting `[DefaultMemberAttribute]`.

### 2.3 Consumption / lookup order

The C# standard's *Indexer access* clause is amended: if normal processing finds no applicable
indexer, an extension indexer access is attempted. The full order (LDM 2026-03-09) is:

1. instance indexers declared or inherited on the receiver type ("real instance indexers");
2. implicit instance indexers (`Index`/`Range` via `Length`/`Count` + `this[int]`/`Slice(int,int)`);
3. real extension indexers;
4. extension implicit indexers.

Extension indexer access proper:
- A candidate extension indexer is *applicable* with respect to receiver `E` and argument list `A`
  if an expanded signature — the type parameters of the extension block plus a parameter list
  combining the extension parameter with the indexer's parameters — is applicable with respect to
  the argument list formed by prepending `E` to `A`.
- The scope walk is the **same one used for extension method invocation**: current and enclosing
  lexical scopes, `using` namespace and `using static` imports.
- Per scope: extension blocks in non-generic static class declarations in that scope contribute
  their indexers; inaccessible and inapplicable candidates are removed; if the set is empty, move to
  the next scope; otherwise run overload resolution; a tie is a compile-time ambiguity error.
- The winning indexer access is then processed as a **static method invocation** of the accessor's
  implementation method, with the receiver as the first argument and the generic arguments inferred
  during the applicability check. Assignment target → `set_Item` implementation; anything else →
  `get_Item` implementation.
- Type inference uses only the receiver and the arguments in the argument list; the **assigned value
  does not contribute** (LDM 2026-02-02).
- Extension members, including extension indexers, are **never** considered when the receiver is a
  `base_access`. Since an *element_access* is only processed as an indexer access when the receiver
  is a variable or value, extension indexers are never considered when the receiver is a type.
- **Never applicable to arrays or `string`** (LDM / Mads by email 2026-04-07) — neither real
  extension indexers nor extension implicit indexers. Declaring such an extension indexer is
  permitted but it can never bind.
- Pointer element access is unaffected (an extension parameter may not be a pointer type).
- Element-access-derived constructs participate automatically: null-conditional element access,
  null-conditional assignment, index assignment in object initializers, list patterns, spread
  elements.
- **Extension indexers cannot be captured in expression trees.**
- Dynamic arguments: an element access with a `dynamic` argument is handled by the element-access
  clause, so it is never processed as an indexer access; the Roslyn test plan lists "Dynamic
  arguments are disallowed".

### 2.4 Knock-on rules for `Length`/`Count`, implicit indexers, list patterns

These extend beyond indexers and are behaviour changes for existing code:

- **Extension `Length`/`Count` properties now make a type countable** and contribute to the implicit
  indexer fallback (LDM 2026-02-02: "extensions should contribute everywhere, including countable
  properties and implicit indexer fallback"). Previously extensions did not participate.
- Lookup for `Length`/`Count` proceeds **scope by scope** (instance scope first, then extension
  scopes), and **within each scope** `Length` is looked for before `Count` (LDM 2026-03-09).
- List patterns resolve `Length`/`Count`, the `Index` indexer and the `Range` indexer independently,
  in this order for the indexers (LDM 2026-03-09):
  a. instance lookup only, find the "real" indexer if possible;
  b. instance lookup only, find the parts of the implicit indexer if possible;
  c. full lookup (instance + extension), find the "real" indexer if possible;
  d. full lookup (instance + extension), find the parts of the implicit indexer (each in individual
     lookups).
- **A classic `this`-parameter extension `Slice` method also contributes** to implicit `Range`
  indexer binding: "yes, we're treating classic and new extension methods exactly the same"
  (LDM 2026-03-09). This can change the meaning of existing code compiled at `LangVersion 15`.
- Extension `Length` does **not** contribute to spread-element size optimization (LDM 2026-03-09).

### 2.5 Metadata / emit

Extension indexers follow the extension-property lowering model. For each CLR-level extension
grouping type containing at least one indexer the compiler emits:

- an extension **property** named `Item` (or the `IndexerNameAttribute` value) whose accessor bodies
  `throw new NotImplementedException()`, carrying `[ExtensionMarkerName]` referencing the extension
  marker type;
- **implementation methods** `get_Item` / `set_Item` in the enclosing static class. These prepend
  the receiver parameter to the parameter list and hold the user-written bodies. They are `static`
  and participate in overload resolution like implementation methods for extension properties;
- `[DefaultMemberAttribute]` on the extension grouping type, with `MemberName` equal to the metadata
  name of the indexer (`Item` by default, or the `IndexerNameAttribute` value).

Speclet example:

```csharp
static class BitExtensions
{
    extension<T>(T t)
    {
        public bool this[int index] { get => ...; set => ...; }
    }
}
```

emits (simplified to C#-like syntax):

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
        public bool this[int index] // extension indexer
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    // accessor implementation methods
    public static bool get_Item<T>(T t, int index) => ...;
    public static void set_Item<T>(T t, int index, bool value) => ...;
}
```

`params` on an extension indexer with a setter: LDM 2026-02-02 decided to **emit the `[ParamArray]`**
on the setter implementation method (option 3), and to verify no negative tooling impact.

### 2.6 Symbol API

**No new public API.** The Roslyn test plan for extension indexers says explicitly
"Public API (no new public APIs)". Extension indexers surface through the existing extension-member
API shipped with C# 14:

```
Microsoft.CodeAnalysis.INamedTypeSymbol.IsExtension.get -> bool
Microsoft.CodeAnalysis.INamedTypeSymbol.ExtensionParameter.get -> IParameterSymbol?
Microsoft.CodeAnalysis.ITypeSymbol.IsExtension.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.ExtensionParameter.get -> IParameterSymbol?
Microsoft.CodeAnalysis.IMethodSymbol.AssociatedExtensionImplementation.get -> IMethodSymbol?
```

An extension indexer is an `IPropertySymbol` with `IsIndexer == true` declared on the extension
grouping type (`INamedTypeSymbol.IsExtension == true`). Its accessors' implementation methods are
reached via `IMethodSymbol.AssociatedExtensionImplementation`. Internally Roslyn uses
`PropertySymbol.IsIndexer && property.IsExtensionBlockMember()`:

```csharp
internal void ReportDisallowedExtensionBlockIndexer(PropertySymbol property, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
{
    if (property.IsIndexer && property.IsExtensionBlockMember() && property.ContainingModule != Compilation.SourceModule)
    {
        MessageID.IDS_FeatureExtensionIndexers.CheckFeatureAvailability(diagnostics, syntax);
    }
}
```

That check fires at **consumption** of an extension indexer imported from another module — i.e.
consuming an extension indexer from a referenced assembly requires `LangVersion >= 15` too.

### 2.7 XML docs / CREF

CREF syntax can refer to an extension indexer, its accessors, and its implementation methods:

```csharp
/// <see cref="E.extension(int).this[string]"/>
/// <see cref="E.extension(int).get_Item(string)"/>
/// <see cref="E.extension(int).get_Item"/>
/// <see cref="E.extension(int).set_Item(string, int)"/>
/// <see cref="E.extension(int).set_Item"/>
/// <see cref="E.get_Item(int, string)"/>
/// <see cref="E.get_Item"/>
/// <see cref="E.set_Item(int, string, int)"/>
/// <see cref="E.set_Item"/>
public static class E
{
    extension(int i)
    {
        /// <summary></summary>
        public int this[string s] { get => throw null; set => throw null; }
    }
}
```

`Binder_Crefs.cs` on `main` performs the `IDS_FeatureExtensionIndexers` check for CREFs.

### 2.8 Extension operators, events, constructors — status

- **Extension operators: C# 14, not C# 15.** They are in the C# 14 grammar
  (`operator_declaration` in `extension_member_declaration`) and have their own C# 14 speclet
  `proposals/csharp-14.0/extension-operators.md`. The learn.microsoft.com `extension` keyword page
  documents operators as available since C# 14.
- **Extension events: not in C# 15.** `event_declaration` appears only in the *"Add support for more
  member kinds"* future-work section of the C# 14 extensions speclet, under the priority list
  "1. Properties and methods, 2. Operators, 3. Indexers, 4. Anything else". There is no
  `extension-events` proposal file in `dotnet/csharplang/proposals` and no entry in the Roslyn
  feature status tables.
- **Extension constructors: not in C# 15.** Also in that future-work section. The C# 14 speclet
  describes the intended design (extension constructors behave like static factory methods, cannot
  have `this`/`base` initializers, would work for interfaces and enums, `new IEnumerable<int>(1, 100)`),
  and explicitly notes that `AttributeTargets.Constructor` was deliberately **excluded** from the
  extension attribute-usage set "as extension constructors would not be constructors".
- **Extension fields, constants, nested types, finalizers, static constructors: not in C# 15.**
  "Extension constants" ([csharplang#10242](https://github.com/dotnet/csharplang/issues/10242),
  Roslyn branch `extension-consts`, [roslyn#84269](https://github.com/dotnet/roslyn/issues/84269))
  is in the Roslyn *Working Set* as **In progress**, i.e. not C# 15.
- Also in the Working Set (not C# 15): "Extension members on typeless receivers"
  ([csharplang#10146](https://github.com/dotnet/csharplang/issues/10146),
  [roslyn#83428](https://github.com/dotnet/roslyn/issues/83428)).

### 2.9 Known-incomplete items (Roslyn test plan #81505, open)

Unchecked at last update (2026-06-19):
- "No implicit `this` accessor in the body" (`Declaration_11_NoImplicitThisReceiverAccess`)
- "Not supported on `base` as a receiver" (`Indexing_38`)
- "Not supported on a type as a receiver" (`Indexing_46`)
- Analyzer actions for extension (`AnalyzerActions_01`)
- **SemanticModel APIs** for `GetSymbolInfo` (`Indexing_01`, `_10`, `_11`) and `LookupSymbols_*`
- `GetMemberGroup` public API
- Interoperability with VB (consumption of implementation methods)
- Unsafe evolution interaction (extension indexers marked as RequiresUnsafe)
- "Check that EnC is blocked"
- Diagnostic quality; IDE features (FAR, AddParameter/ChangeSignature, Rename,
  RemoveUnusedValueAssignment, RemoveUnusedMembers)

`IOperation`/flow graph, nullable analysis, ref-safety analysis, metadata production and consumption,
symbol display, and CREF are marked done.

---

## 3. Labeled `break` and `continue`

### 3.1 Grammar

Standard diff (against C# 7 standard `statements.md`):

```ANTLR
break_statement
    : 'break' identifier? ';'
    ;

continue_statement
    : 'continue' identifier? ';'
    ;
```

Roslyn's test plan writes it with the attribute lists Roslyn also models:

```antlr
break_statement
  : attribute_list* 'break' identifier_name? ';'
  ;

continue_statement
  : attribute_list* 'continue' identifier_name? ';'
  ;
```

No new syntax node type and no new `SyntaxKind`. A labeled loop is just the existing
`LabeledStatementSyntax` wrapping the loop.

### 3.2 Which statements may carry a label, and which may be targeted

Amendment to §13.5 *Labeled statements*:

> If the *statement* immediately nested within a *labeled_statement* is a *switch_statement* or an
> *iteration_statement*, the nested statement is said to be *labeled with* the *identifier* of the
> *labeled_statement*. A *break_statement* or *continue_statement* can specify such an *identifier*
> to reference the containing labeled statement.

> **Note**: Only the *statement* that is **immediately** nested within a *labeled_statement* is
> labeled with that identifier. For example, given `a: b: while (…) …`, only `b` labels the
> *iteration_statement*; `a` labels the inner *labeled_statement* `b: while (…) …`, which is not
> itself a *switch_statement* or *iteration_statement*. Consequently, `break a;` or `continue a;`
> appearing within the loop body does not target the `while` statement. **end note**

So:
- Labels usable as a `break` target: `switch`, `while`, `do`, `for`, `foreach` (a labelled
  `foreach await` is an iteration statement too).
- Labels usable as a `continue` target: `while`, `do`, `for`, `foreach` only — **`continue` never
  targets a `switch`.**
- The label must be **immediately** on the loop/switch. Stacked labels do not chain.

§13.10.2 *break*:

> The `break` statement exits the nearest enclosing *switch_statement* or *iteration_statement*, or,
> if an *identifier* is specified, the nearest enclosing *switch_statement* or *iteration_statement*
> labeled with that *identifier*. … If no such enclosing statement exists, a compile-time error
> occurs.

§13.10.3 *continue* is the analogue for *iteration_statement* only.

Unchanged: a `break`/`continue` cannot exit a `finally` block; when one occurs inside a `finally`
block its target must be within the same `finally` block. Intervening `try`/`finally` blocks run in
the usual order before control transfers.

Examples from the speclet:

```csharp
outer: for (int i = 0; i < 10; i++)
{
    for (int j = 0; j < 10; j++)
    {
        if (i * j > 20)
            break outer; // exits the outer for-loop
    }
}
```

```csharp
outer: for (int i = 0; i < 10; i++)
{
    for (int j = 0; j < 10; j++)
    {
        if (ShouldSkip(i, j))
            continue outer; // continues the outer for-loop
    }
}
```

### 3.3 Roslyn syntax model

`Syntax.xml` on `main` — the `Name` field is a new **optional node**, positioned between the
keyword and the semicolon:

```xml
<Node Name="BreakStatementSyntax" Base="StatementSyntax">
  <Kind Name="BreakStatement"/>
  <Field Name="AttributeLists" Type="SyntaxList&lt;AttributeListSyntax&gt;" Override="true"/>
  <Field Name="BreakKeyword" Type="SyntaxToken"><Kind Name="BreakKeyword"/></Field>
  <Field Name="Name" Type="IdentifierNameSyntax" Optional="true"/>
  <Field Name="SemicolonToken" Type="SyntaxToken"><Kind Name="SemicolonToken"/></Field>
</Node>
<Node Name="ContinueStatementSyntax" Base="StatementSyntax">
  <Kind Name="ContinueStatement"/>
  <Field Name="AttributeLists" Type="SyntaxList&lt;AttributeListSyntax&gt;" Override="true"/>
  <Field Name="ContinueKeyword" Type="SyntaxToken"><Kind Name="ContinueKeyword"/></Field>
  <Field Name="Name" Type="IdentifierNameSyntax" Optional="true"/>
  <Field Name="SemicolonToken" Type="SyntaxToken"><Kind Name="SemicolonToken"/></Field>
</Node>
```

New public API (PublicAPI.Unshipped.txt):

```
BreakStatementSyntax.Name.get -> IdentifierNameSyntax?
BreakStatementSyntax.WithName(IdentifierNameSyntax? name) -> BreakStatementSyntax!
BreakStatementSyntax.Update(SyntaxList<AttributeListSyntax!> attributeLists, SyntaxToken breakKeyword, IdentifierNameSyntax? name, SyntaxToken semicolonToken) -> BreakStatementSyntax!
ContinueStatementSyntax.Name.get -> IdentifierNameSyntax?
ContinueStatementSyntax.WithName(IdentifierNameSyntax? name) -> ContinueStatementSyntax!
ContinueStatementSyntax.Update(SyntaxList<AttributeListSyntax!> attributeLists, SyntaxToken continueKeyword, IdentifierNameSyntax? name, SyntaxToken semicolonToken) -> ContinueStatementSyntax!
static SyntaxFactory.BreakStatement(IdentifierNameSyntax? name = null) -> BreakStatementSyntax!
static SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!> attributeLists, IdentifierNameSyntax? name) -> BreakStatementSyntax!
static SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!> attributeLists, SyntaxToken breakKeyword, IdentifierNameSyntax? name, SyntaxToken semicolonToken) -> BreakStatementSyntax!
static SyntaxFactory.ContinueStatement(IdentifierNameSyntax? name = null) -> ContinueStatementSyntax!
static SyntaxFactory.ContinueStatement(SyntaxList<AttributeListSyntax!> attributeLists, IdentifierNameSyntax? name) -> ContinueStatementSyntax!
static SyntaxFactory.ContinueStatement(SyntaxList<AttributeListSyntax!> attributeLists, SyntaxToken continueKeyword, IdentifierNameSyntax? name, SyntaxToken semicolonToken) -> ContinueStatementSyntax!
```

The previously shipped overloads are retained (`PublicAPI.Shipped.txt`):

```
static SyntaxFactory.BreakStatement() -> BreakStatementSyntax!
static SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!> attributeLists) -> BreakStatementSyntax!
static SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!> attributeLists, SyntaxToken breakKeyword, SyntaxToken semicolonToken) -> BreakStatementSyntax!
static SyntaxFactory.BreakStatement(SyntaxToken breakKeyword, SyntaxToken semicolonToken) -> BreakStatementSyntax!
BreakStatementSyntax.Update(SyntaxList<AttributeListSyntax!>, SyntaxToken, SyntaxToken) -> BreakStatementSyntax!
BreakStatementSyntax.Update(SyntaxToken, SyntaxToken) -> BreakStatementSyntax!
```
(and the `ContinueStatement` equivalents). So `SyntaxFactory.BreakStatement()` still binds to the
zero-parameter overload, and the 3-argument `Update` still exists next to the new 4-argument one.
The Roslyn test plan draft showed the 4-argument factory decorated with
`[Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "…/issues/83266")]`, but the
API as recorded in `PublicAPI.Unshipped.txt` on `main` carries **no** experimental marker; public API
review is still tracked by [roslyn#83266](https://github.com/dotnet/roslyn/issues/83266).

### 3.4 Parsing is unconditional (no language-version gate)

`LanguageParser` on `main`:

```csharp
private BreakStatementSyntax ParseBreakStatement(SyntaxList<AttributeListSyntax> attributes)
{
    return _syntaxFactory.BreakStatement(
        attributes,
        this.EatToken(SyntaxKind.BreakKeyword),
        this.IsTrueIdentifier() ? this.ParseIdentifierName() : null,
        this.EatToken(SyntaxKind.SemicolonToken));
}

private ContinueStatementSyntax ParseContinueStatement(SyntaxList<AttributeListSyntax> attributes)
{
    return _syntaxFactory.ContinueStatement(
        attributes,
        this.EatToken(SyntaxKind.ContinueKeyword),
        this.IsTrueIdentifier() ? this.ParseIdentifierName() : null,
        this.EatToken(SyntaxKind.SemicolonToken));
}
```

The Roslyn test plan states it as a design point: "LangVer (unconditional parsing, check during
binding)". This is not a compat break — `break x;` was never valid C# before.

### 3.5 Binding

`Binder_Statements.cs` on `main`:

```csharp
var labelName = name?.Identifier.ValueText;
if (labelName != null)
{
    MessageID.IDS_FeatureLabeledBreakContinue.CheckFeatureAvailability(diagnostics, node, name.GetLocation());
}

var hasErrors = false;
LabelSymbol? target = isBreak ? this.GetBreakLabel(labelName) : this.GetContinueLabel(labelName);

// If we didn't get a target, still try to bind the label name to get a BoundLabel for error recovery.
BoundLabel? label = name == null ? null : BindLabel(name, target != null ? diagnostics : BindingDiagnosticBag.Discarded) as BoundLabel;
if (target is null)
{
    Error(diagnostics,
        labelName != null ? (isBreak ? ErrorCode.ERR_NoBreakId : ErrorCode.ERR_NoContinueId) : ErrorCode.ERR_NoBreakOrCont,
        name ?? (SyntaxNode)node,
        labelName == null ? [] : [labelName]);

    target = label?.Label;
    hasErrors = true;
}

if (target is null)
    return new BoundBadStatement(node, childBoundNodes: [], hasErrors);

return isBreak
    ? new BoundBreakStatement(node, target, label, hasErrors)
    : new BoundContinueStatement(node, target, label, hasErrors);
```

`BoundBreakStatement` / `BoundContinueStatement` gained a `Label` (`BoundLabel?`) member in addition
to the existing target `LabelSymbol` — this is what backs `GetSymbolInfo` on the `Name` node
(returning an `ILabelSymbol`).

New `Binder` virtuals (replacing the previous `BreakLabel` / `ContinueLabel` properties, which now
delegate):

```csharp
internal GeneratedLabelSymbol? BreakLabel => GetBreakLabel(labelName: null);
internal GeneratedLabelSymbol? ContinueLabel => GetContinueLabel(labelName: null);

internal virtual GeneratedLabelSymbol? GetBreakLabel(string? labelName) => Next.GetBreakLabel(labelName);
internal virtual GeneratedLabelSymbol? GetContinueLabel(string? labelName) => Next.GetContinueLabel(labelName);
```

`LoopBinderContext.cs` — the whole file, which shows exactly how a label is attached to a loop:

```csharp
internal abstract class LoopBinder : LocalScopeBinder
{
    private readonly GeneratedLabelSymbol _breakLabel;
    private readonly GeneratedLabelSymbol _continueLabel;
    private readonly string? _labelName;

    protected LoopBinder(Binder enclosing, SyntaxNode loopSyntax)
        : base(enclosing)
    {
        _breakLabel = new GeneratedLabelSymbol("break");
        _continueLabel = new GeneratedLabelSymbol("continue");
        _labelName = loopSyntax.Parent is LabeledStatementSyntax labeled ? labeled.Identifier.ValueText : null;
    }

    internal override GeneratedLabelSymbol? GetBreakLabel(string? labelName)
        => (labelName is null || labelName == _labelName) ? _breakLabel : NextRequired.GetBreakLabel(labelName);

    internal override GeneratedLabelSymbol? GetContinueLabel(string? labelName)
        => (labelName is null || labelName == _labelName) ? _continueLabel : NextRequired.GetContinueLabel(labelName);
}
```

`loopSyntax.Parent is LabeledStatementSyntax` is the mechanical realisation of the
"immediately nested" rule. `SwitchBinder` has the analogous `GetBreakLabel` override.
Other overriders: `BuckStopsHereBinder`, `InMethodBinder`.

### 3.6 New diagnostics

| Code | ErrorCode | Message |
|---|---|---|
| CS9393 | `ERR_NoBreakId` | `No enclosing loop or switch statement with the label '{0}' out of which to break` |
| CS9394 | `ERR_NoContinueId` | `No enclosing loop with the label '{0}' out of which to continue` |

The unlabeled cases keep `ERR_NoBreakOrCont` (CS0139).

### 3.7 Lowering / codegen

There is **no special lowering**. Binding resolves the label to the `GeneratedLabelSymbol` that the
targeted loop or switch already owns for its break/continue points, so `break outer;` emits exactly
the branch a plain `break;` in that loop would emit. The Roslyn test plan records this explicitly:

> Runtime: check the IL we generate makes the JIT's loop recognition happy (same code as `goto`)

Consequently there is no new `IOperation` node and no change to the control-flow graph shape — a
labeled `break`/`continue` is still an `IBranchOperation` (`BranchKind.Break`/`BranchKind.Continue`)
whose `Target` is the loop's label. The test plan marks IOperation, CFG, semantic model
(`GetSymbolInfo`), data-flow analysis (definite assignment, unused-label reporting, nullability),
and `NormalizeWhitespace` as done; `LookupSymbol` is marked "unchanged, N/A".

### 3.8 IDE0410 — "Use labeled jump statement"

<https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0410>

| Property | Value |
|---|---|
| Rule ID | IDE0410 |
| Title | Use labeled jump statement |
| Category | Style |
| Subcategory | Language rules (code-block preferences) |
| Applicable languages | C# 15+ |
| Option | `csharp_style_prefer_labeled_jump_statements` |
| Option values | `true` (prefer labeled jump statements) / `false` (disables the rule) |
| Default | `true` |

Patterns detected:
1. a `goto` that jumps to a label immediately after a nested loop → `break <label>`;
2. a `goto` that jumps to an empty label at the end of the loop body → `continue <label>`;
3. a Boolean flag set in an inner loop and checked at each outer level to propagate a break or
   continue outward → a single labeled `break <label>` / `continue <label>`.

Documented before/after (abbreviated):

```csharp
// violation
for (int x = 0; x < 10; x++)
    for (int y = 0; y < 10; y++)
        if (x * y > 20) goto found;
found:
Console.WriteLine("Done");

// fixed
found: for (int x = 0; x < 10; x++)
    for (int y = 0; y < 10; y++)
        if (x * y > 20) break found;
Console.WriteLine("Done");
```

```csharp
// violation
bool found = false;
for (int i = 0; i < 10; i++)
{
    for (int j = 0; j < 10; j++)
        if (i * j > 20) { found = true; break; }
    if (found) break;
}

// fixed
loop_i: for (int i = 0; i < 10; i++)
    for (int j = 0; j < 10; j++)
        if (i * j > 20) break loop_i;
```

Suppression: `#pragma warning disable IDE0410` / `dotnet_diagnostic.IDE0410.severity = none`.

### 3.9 Open spec questions in the speclet

Two "Open questions" remain in `labeled-break-continue.md`:

1. **Label semantics.** Whether to formalise `break identifier` as "find the innermost applicable
   labeled loop/switch" (the current formalisation, and what Roslyn implements via
   `LoopBinder._labelName`) or as "resolve the identifier like `goto` does, then check the label
   directly contains an enclosing loop/switch". The speclet says both allow and disallow exactly the
   same programs. The illustrative case:
   ```csharp
   void M()
   {
     label:
     Console.WriteLine();

     foreach (var x in ...)
     {
       break label;
       // should this fail because (1) identifier lookup fails, or (2) the label is found but rejected?
     }
   }
   ```
2. **Nested labels.** Should `a: b: while (true) continue a;` be supported? The speclet's
   recommendation is **no**, and Roslyn implements "no" (the loop's `Parent` must be the
   `LabeledStatementSyntax`). The Roslyn test plan marks both spec items as resolved.

The "Design meetings" section of the speclet is still "TBD".

---

## 4. Consolidated source list

Roslyn `main` (raw file reads, 2026-09-03):
- `docs/Language Feature Status.md`
- `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`
- `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
- `src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`, `PublicAPI.Shipped.txt`
- `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt`, `PublicAPI.Shipped.txt`
- `src/Compilers/Core/Portable/Operations/OperationInterfaces.xml`
- `src/Compilers/CSharp/Portable/Errors/MessageID.cs`, `Errors/ErrorCode.cs`, `CSharpResources.resx`
- `src/Compilers/CSharp/Portable/LanguageVersion.cs`
- `src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`
- `src/Compilers/CSharp/Portable/Binder/Binder.cs`, `Binder_Expressions.cs`, `Binder_Statements.cs`,
  `LoopBinderContext.cs`
- `src/Compilers/CSharp/Portable/Compilation/CSharpSemanticModel.cs`

csharplang `main`:
- `proposals/csharp-15.0/collection-expression-arguments.md`
- `proposals/csharp-15.0/extension-indexers.md`
- `proposals/csharp-15.0/labeled-break-continue.md`
- `proposals/csharp-14.0/extensions.md`

learn.microsoft.com:
- `/dotnet/csharp/whats-new/csharp-15`
- `/dotnet/csharp/whats-new/breaking-changes/compiler breaking changes - dotnet 11`
- `/dotnet/csharp/language-reference/operators/collection-expressions`
- `/dotnet/csharp/language-reference/keywords/extension`
- `/dotnet/csharp/language-reference/statements/jump-statements`
- `/dotnet/fundamentals/code-analysis/style-rules/ide0410`

GitHub issues/PRs: roslyn#80613, roslyn#81505, roslyn#83209, roslyn#83266, roslyn#81860,
roslyn#84269, roslyn#83428, PR roslyn#84271; csharplang#8887, #9856, #9875, #8659, #10146, #10242.

---

## 5. Open questions / unresolved contradictions

1. **`with(...)` under `LangVersion < 15`.** LDM-2025-03-17 and the published breaking-change note
   say the pre-C#-15 behaviour ("call to a method named `with`") is preserved for earlier language
   versions. Roslyn `main` as of 2026-09-03 parses `with (` at the start of a collection element as
   a `WithElementSyntax` **unconditionally** (`LanguageParser.ParseCollectionElement`, no version
   check) and the binder always treats it as a with-element, only reporting
   `IDS_FeatureCollectionExpressionArguments.CheckFeatureAvailability`. I found no code path that
   re-binds it as an invocation at `LangVersion 12/13/14`. Either the docs describe intent that the
   implementation does not (yet) match, or the fallback lives somewhere I did not locate. The
   speclet still lists this as an open question ("Finalizing an open concern from LDM-2025-03-17").
   Needs a direct compiler experiment against a .NET 11 preview SDK with `-langversion:14`.
2. **"C# 16" in the .NET 11 breaking-changes document.** Roslyn's own
   `Compiler Breaking Changes - DotNet 11.md` describes the VS 18.7 / 18.9 unsafe-evolution items as
   "In C# 16" and `langversion:16`, yet `LanguageVersion.cs` on `main` has no `CSharp16` member and
   `CurrentVersion` is `CSharp15`. Most likely those notes are forward-labelling features that are
   currently gated behind `LanguageVersion.Preview`, but I could not confirm this from a primary
   source.
3. **`__arglist` in `with(...)`.** LDM-2025-04-14 resolved "No support for `__arglist` in collection
   arguments unless free", but `bindWithElement` calls
   `BindArgumentsAndNames(withElementSyntax.ArgumentList, diagnostics, analyzedArguments, allowArglist: true)`.
   Whether `[with(__arglist(x, y))]` is accepted in practice was not verified.
4. **Which .NET 11 preview shipped each feature.** I established the Visual Studio 2026 version for
   the `with(...)` behaviour change (18.4) but did not map VS 18.x versions to .NET 11 preview
   numbers, so I cannot state "collection expression arguments shipped in .NET 11 Preview N".
5. **Extension indexers and `SemanticModel.GetSymbolInfo` / `GetMemberGroup`.** These are explicitly
   unchecked in the open test plan roslyn#81505. Their behaviour at GA is not yet determined.
   Likewise "Check that EnC is blocked" is unresolved, which matters for any tool that edits code at
   design time.
6. **Whether extension indexers ship as fully stable at GA.** The language-version gate says
   `CSharp15` (stable), but the test plan is still open with substantive compiler items unchecked
   (base-receiver rejection, type-receiver rejection, analyzer actions, VB interop). It is possible
   some of these are simply untested rather than unimplemented.
7. **`ERR_CollectionRefLikeElementType` (CS9358).** It was added in the same error-code block as the
   collection-argument errors, but I did not establish which language rule reports it (it reads like
   a general collection-expression rule about ref-struct element types rather than a `with()` rule).
8. **Stale error numbers in Roslyn's own test files.** `CollectionExpressionTests_WithElement_Constructor.cs`
   comments reference CS9335 / CS9337 while `ErrorCode.cs` on `main` defines 9354 / 9356. I treated
   `ErrorCode.cs` as authoritative but did not confirm the final numbers against a shipped compiler.
