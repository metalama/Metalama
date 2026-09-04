# 12. Draft application programming interfaces for the C# 15 features

## What this document is, and what it is not

This is a draft written to be criticised, in the sense of section 11 of
[`DECISIONS.md`](../DECISIONS.md). It exists because the concepts of the C# 15
work stay abstract until a shape is on the page, and a shape on the page is what makes a trade-off arguable. It is
illustrative material. It is not a specification, it carries no authority over the user stories, and a story does not
become a specification by citing it. Section 11 of the same document keeps the rule that a story states the
capability, the scope and the acceptance criteria, and not the shape of the interface.

The document is written the way an aspect author meets the feature: the code the author writes, the code the author's
users write, the code that Metalama produces, and the diagnostic the author sees when it fails.

It covers five subjects: reading a union, reading a closed hierarchy, extension indexers, the diagnostic for a
labeled `break` or `continue` in a template, and the divergence on the Roslyn 5.0 variant. It does not cover the
introduction of a union or of a union leg. Section 4 of `DECISIONS.md` makes that work required and gives it to a
separate analysis; nothing here designs it, and nothing here should be read as contradicting it. Where a reading
member drafted here would also have to appear on a builder, this document says so and stops.

No project was built and no test was run. Every claim about existing code carries a file and a line. Where a claim
rests on an external premise that could not be checked in this session, the text says so and names what would settle
it.

Two constraints from `DECISIONS.md` shape every draft below and are not repeated in each section.

- Section 6: the Roslyn 5.12 members are reached through preprocessor blocks in the latest variant only, and
  `Metalama.Framework`, the public application programming interface assembly, is not built per variant. A member
  drafted here therefore exists in every host, while the engine code that answers it is compiled only in the latest
  variant. The lower variant must return a defined value, not throw.
- Section 5: the template language stays at C# 14, and a labeled `break` or `continue` in a template is forbidden and
  must be reported.

---

## 1. Reading a union

### The precedent to follow

`IsRecord` is the model. It is a single boolean on `INamedType`, documented in two lines, with no value of its own in
the `TypeKind` enumeration:

```csharp
// Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs:199-202
/// <summary>
/// Gets a value indicating whether type is a record. Also returns <c>false</c> when the type neither a class nor a record.
/// </summary>
bool IsRecord { get; }
```

The two `TypeKind` values that records once had are obsolete with `error: true`, and their obsolescence messages name
the replacement:

```csharp
// Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs:29-30
[Obsolete( "TypeKind.Class and INamedType.IsRecord", true )]
RecordClass,

// Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs:67-68
[Obsolete( "TypeKind.Struct and INamedType.IsRecord", true )]
RecordStruct,
```

A union follows the same physics. Roslyn lowers a C# 15 union to a struct, so `SourceNamedTypeImpl.TypeKind`
(`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79`) already returns
`TypeKind.Struct` and no switch over `TypeKind` throws (finding CM-1 of
[`03-code-model-unions-closed.md`](../03-code-model-unions-closed.md)). Adding
`TypeKind.Union` would repeat the record mistake and require a new arm in every exhaustive switch over the Metalama
`TypeKind`. The flag is the right shape, and it is not an open question.

### What the aspect author writes

```csharp
public sealed class VisitorAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.Target;

        if ( !type.IsUnion )
        {
            builder.SkipAspect();

            return;
        }

        // The declared cases, in source order. These are pre-existing types named in the union header;
        // no case type is synthesized.
        foreach ( var caseType in type.UnionCaseTypes )
        {
            builder.IntroduceMethod(
                nameof(this.VisitTemplate),
                buildMethod: m => m.Name = "Visit" + ((INamedType) caseType).Name,
                args: new { TCase = caseType } );
        }

        // The compiler-synthesized 'public object? Value { get; }' property.
        var valueProperty = type.Properties.OfName( "Value" ).Single();

        // The compiler-synthesized public constructors, one per case type.
        foreach ( var constructor in type.Constructors )
        {
            var caseType = constructor.Parameters[0].Type;
        }
    }

    [Template]
    public bool VisitTemplate( [CompileTime] IType tCase )
    {
        return meta.Target.Type.Properties.OfName( "Value" ).Single().Value is not null;
    }
}
```

### What the author's user writes

```csharp
[Visitor]
public union Shape
{
    case Circle;
    case Rectangle;
}

public record Circle( double Radius );
public record Rectangle( double Width, double Height );
```

### The draft members

```csharp
// Added to Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs, beside IsRecord at :202.

/// <summary>
/// Gets a value indicating whether the type is a union. Returns <c>false</c> for any other type.
/// </summary>
/// <remarks>
/// <para>
/// A union is independent of <see cref="IType.TypeKind"/>. A union declared with the <c>union</c> keyword is
/// reported by the compiler as a struct, and a class or a struct that carries the
/// <c>System.Runtime.CompilerServices.UnionAttribute</c> is also a union, so a union is not necessarily a value type.
/// </para>
/// <para>
/// This property returns <c>false</c> in a host whose Roslyn version predates C# 15, which is described in
/// <see cref="INamedType.IsClosed"/>.
/// </para>
/// </remarks>
bool IsUnion { get; }

/// <summary>
/// Gets the case types of a union, in the order in which they are declared. Returns an empty list when
/// <see cref="IsUnion"/> is <c>false</c>.
/// </summary>
/// <remarks>
/// The case types are types declared elsewhere and named in the union header. No case type is synthesized by the
/// compiler, and a case type is an ordinary named type with no distinguishing member of its own.
/// </remarks>
IReadOnlyList<IType> UnionCaseTypes { get; }
```

### What each member returns for a type that is not a union

| Member | Non-union source type | Introduced type or builder | Roslyn 5.0 variant |
| --- | --- | --- | --- |
| `IsUnion` | `false` | `false` | `false` for every type |
| `UnionCaseTypes` | empty list | empty list | empty list for every type |
| `TypeKind` | unchanged | unchanged | `Struct` for a union read from metadata |
| `IsRecord` | unchanged | unchanged | unchanged |
| `Constructors` | unchanged | unchanged | lists the per-case constructors |
| `Properties` | unchanged | unchanged | lists the `Value` property |

The implementation sites are `SourceNamedTypeImpl` for the source type, the facade at
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedType.cs:514-522`, which forwards
`IsRecord` through `OnUsingDeclaration` and is the pattern to copy, and the constant `false` and empty list in
`NamedTypeBuilder.cs:36`, `NamedTypeBuilderData.cs:55`, `IntroducedNamedType.cs:200` and
`IntroducedExtensionBlock.cs:190` (all cited in CM-1). Whether `INamedTypeBuilder` gains a settable counterpart is
the subject of the union introduction analysis and is not decided here.

### Two shapes, and the one I would take

The choice that matters is how the synthesized members are reached. Nothing in Roslyn marks the `Value` property:
its symbol class is internal, so the member is discoverable only by name and signature (CM-1, open questions). Two
shapes follow.

Shape A, the flat one drafted above. The author writes `type.Properties.OfName( "Value" ).Single()` and
`type.Constructors`. The advantage is that it adds two members and no new concept, and every existing collection
member keeps working. The disadvantage is that the author repeats a magic string, and that a union with a
user-declared member named `Value` would make `Single()` throw. That second case is not hypothetical for an
attribute-based union, where the user writes the type by hand.

Shape B, a bundle:

```csharp
/// <summary>
/// Gets the union facet of the type, or <c>null</c> when the type is not a union.
/// </summary>
IUnionInfo? Union { get; }

[CompileTime]
public interface IUnionInfo
{
    IReadOnlyList<IType> CaseTypes { get; }

    /// <summary>Gets the compiler-synthesized <c>Value</c> property.</summary>
    IProperty Value { get; }

    /// <summary>Gets the compiler-synthesized constructor for a given case type.</summary>
    IConstructor GetConstructor( IType caseType );
}
```

Shape B removes the magic string, gives the two synthesized members a name that survives a rename in a future
language revision, and makes `if ( type.Union is { } union )` the single branch that the author writes. Its cost is a
new public interface, a new implementation in each of the five sites above, and a departure from the `IsRecord`
precedent that the theme document explicitly recommends following.

I would take shape A for `IsUnion` and `UnionCaseTypes`, because the precedent is strong and the members are cheap,
and I would add the two synthesized members as extension methods on `NamedTypeExtensions` rather than as a bundle:
`type.GetUnionValueProperty()` and `type.GetUnionConstructor( caseType )`. That keeps the interface additions to two
and still removes the magic string from author code. The argument against my own choice is that an extension method
cannot be implemented differently by the builder and the source type, so a future writer story would have to move the
members onto the interface anyway; if the discussion expects the writer soon, shape B is the cheaper path overall.

### What the author sees when it fails

An aspect that treats a union as an ordinary struct and introduces an instance field is refused by an eligibility
rule that has to be added (CM-1, proposed change), reported through
`GeneralDiagnosticDescriptors.AspectNotEligibleOnTarget`
(`Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:151-156`):

```
error LAMA0037: The aspect 'MyAspect' cannot be applied to the named type 'Shape' because a field cannot be
introduced into a union.
```

Without that rule, the member is dropped by the linker injection rewriter with no diagnostic at all, because
`LinkerInjectionStep.Rewriter` has no `VisitUnionDeclaration` override
(`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:316-324` and `:359`,
finding LK-1 of [`04-linker-and-advice.md`](../04-linker-and-advice.md)). The
eligibility rule is therefore not a nicety; it is what turns a silent wrong output into a diagnostic.

### Uncertainty

`ITypeSymbol.UnionCaseTypes` does not exist in the Roslyn build that Metalama consumes today. It was added to
`dotnet/roslyn` main on 2026-07-31, after the 2026-07-15 build `5.10.0-1.26365.3` that the latest variant binds
against (`FACTS.md`, addendum). `ITypeSymbol.IsUnion` does exist there but carries `RSEXPERIMENTAL006`. What would
settle the shape of `UnionCaseTypes` is the stable Roslyn 5.12 reference assembly: whether the member returns
`ImmutableArray<ITypeSymbol>` in declaration order, and whether it is defined for an attribute-based union as well as
for a `union` declaration. Until then, `IsUnion` can ship alone and `UnionCaseTypes` cannot.

---

## 2. Reading a closed hierarchy

### The finding first: no new derived-types member is warranted

This section concludes that the existing derived type index already answers the question, and that the only new
public member is the flag itself. I state that plainly because it is the useful outcome.

Closedness is exactly the guarantee that the set of derived types is complete and that the derived types are in the
same assembly. The Metalama derived type index already restricts itself to the current compilation. Every candidate
passes through `IsContainedInCurrentCompilation`:

```csharp
// Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/DerivedTypeIndex.cs:117-126
private IEnumerable<IFullRef<INamedType>> GetDirectlyDerivedTypesCore( IFullRef<INamedType> baseType )
{
    foreach ( var namedType in this.GetRelationships( baseType ) )
    {
        if ( this.IsContainedInCurrentCompilation( namedType ) )
        {
            yield return namedType.ToRef( baseType.RefFactory );
        }
    }
}
```

and `DerivedTypesOptions.DirectOnly` is documented as "Only returns types declared in the current compilation that
directly derive from the given type" (`Metalama.Framework/src/Metalama.Framework/Code/DerivedTypesOptions.cs:27-30`).
For a closed type declared in the current compilation, that set is the complete set, because the language requires
every subtype of a closed type to be in the same module. Nothing new is needed to enumerate it.

Two qualifications, both from finding CM-5:

- The completeness holds only when the compilation model is built on a complete compilation
  (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/PartialCompilation.cs:449-460`, which indexes every
  type of the assembly). A partial compilation indexes only the closure of the selected syntax trees
  (`:237-280` with `:366-420`), so `DirectOnly` can be short there.
- For a closed type that comes from a referenced assembly, no option answers. `All` and `DirectOnly` exclude external
  types by construction, and `IncludingExternalTypesDangerous` is documented as incomplete
  (`DerivedTypesOptions.cs:39-44`).

### What the aspect author writes

```csharp
public sealed class ExhaustiveDispatchAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.Target;

        if ( !type.IsClosed )
        {
            builder.Diagnostics.Report( _mustBeClosed.WithArguments( type ) );

            return;
        }

        // Complete, because the type is closed and declared in this compilation.
        var cases = builder.Target.Compilation
            .GetDerivedTypes( type, DerivedTypesOptions.DirectOnly )
            .ToList();

        builder.IntroduceMethod( nameof(this.DispatchTemplate), args: new { cases } );
    }

    [Template]
    public void DispatchTemplate( [CompileTime] IReadOnlyList<INamedType> cases )
    {
        foreach ( var c in meta.CompileTime( cases ) )
        {
            if ( meta.This is not null ) { }
        }
    }
}
```

### What the author's user writes

```csharp
[ExhaustiveDispatch]
public closed class Shape
{
    public sealed class Circle : Shape;
    public sealed class Rectangle : Shape;
}
```

### The draft member

```csharp
// Added to Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs, beside IsRecord at :202.

/// <summary>
/// Gets a value indicating whether the type is declared with the <c>closed</c> modifier. Returns <c>false</c> for
/// any other type.
/// </summary>
/// <remarks>
/// <para>
/// A closed type is implicitly abstract, so <see cref="IMemberOrNamedType.IsAbstract"/> is <c>true</c> and
/// <see cref="IMemberOrNamedType.IsSealed"/> is <c>false</c> for it.
/// </para>
/// <para>
/// When the type is closed and is declared in the current compilation, the set returned by
/// <see cref="ICompilation.GetDerivedTypes(INamedType, DerivedTypesOptions)"/> with
/// <see cref="DerivedTypesOptions.DirectOnly"/> is the complete set of its direct subtypes, because the language
/// requires every subtype of a closed type to be declared in the same module. This does not hold for a closed type
/// declared in a referenced assembly, nor when the compilation model is built on a partial compilation.
/// </para>
/// <para>
/// This property returns <c>false</c> in a host whose Roslyn version predates C# 15. See the design-time note in the
/// documentation of the supported platforms.
/// </para>
/// </remarks>
bool IsClosed { get; }
```

The one further edit is documentation rather than interface: the paragraph above belongs also on
`DerivedTypesOptions.DirectOnly` (`DerivedTypesOptions.cs:27-30`), because that is where an author looks when
deciding which option to pass.

### The shape I rejected, and why the argument is not closed

The alternative is a new option or a new member that says "the complete set":

```csharp
// Rejected.
public enum DerivedTypesOptions
{
    // ...
    /// <summary>Returns the complete set of derived types. Valid only for a closed type.</summary>
    Exhaustive
}
```

Against it: for a closed type of the current compilation it is `DirectOnly` under a second name, and a second name
for one behaviour is a maintenance cost with no capability behind it. It also invites the reading that `DirectOnly`
is somehow less complete for a closed type, which is false.

For it: it would be the natural place to hang the one case that `DirectOnly` genuinely cannot answer, a closed type
from a referenced assembly, for which Roslyn's `GetClosedDerivedTypeInfo` is the only complete source, because Roslyn
scans the type definitions of the referenced module (CM-5). If that case is in scope, a member is warranted; and it
must honour the completeness flag that the Roslyn result carries, because the result is incomplete when a generic
closed type has a derived type that cannot be named, and because Roslyn returns derived type definitions whereas
exhaustiveness for a particular construction of a generic closed type is narrower.

I would ship `IsClosed` and the documentation, and nothing else, and treat the external closed type as a separate
question that the discussion should answer rather than a shape to design now. `DirectOnly` returns an empty sequence
for such a type today, which is wrong but silent, so the honest minimum is to say in the documentation of `IsClosed`
that the completeness guarantee does not extend to it.

### What the author sees when it fails

There is no diagnostic today for the case that matters, which is the silent one: on the Roslyn 5.0 variant
`IsClosed` returns `false`, the aspect above reports its own "must be closed" diagnostic in the editor and not at the
command line, and the two disagree. That is subject 5.

---

## 3. Extension indexers

### Overriding: the author writes what already works

An extension indexer adds no syntax node, no syntax kind and no symbol member. It is an ordinary indexer declaration
inside an extension block, and it surfaces as a property symbol with the indexer flag on a type of the extension kind
(finding LK-7). The override path is the extension property path, and the advice factory already routes it: the
receiver substitution for an extension block is at
`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAspectReferenceSyntaxProvider.cs:213-218`.

What the author writes today for an extension property, unchanged, is the shape for an indexer. The existing
extension property override test is the reference:

```csharp
// Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/
//   ExtensionMembers/ExtensionMembers_OverrideProperty.cs:15-30
internal class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( $"Member: {meta.Target.Method}" );

            if ( meta.Target.Method.HasReceiver() )
            {
                Console.WriteLine( meta.Receiver );
            }

            return meta.Proceed();
        }
        // set omitted
    }
}
```

For an indexer there is no `OverrideIndexerAspect`; the author uses `OverrideAccessors`, exactly as for an ordinary
indexer (`Tests/Aspects/Overrides/Indexers/NotInlineable.cs:15-22`):

```csharp
public sealed class TraceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var block in builder.Target.ExtensionBlocks )
        {
            foreach ( var indexer in block.Indexers )
            {
                builder.With( indexer ).OverrideAccessors( nameof(this.GetTemplate), nameof(this.SetTemplate) );
            }
        }
    }

    [Template]
    public dynamic? GetTemplate()
    {
        Console.WriteLine( $"Get {meta.Target.Method}" );
        Console.WriteLine( meta.Receiver );

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate()
    {
        Console.WriteLine( $"Set {meta.Target.Method}" );
        meta.Proceed();
    }
}
```

### What the author's user writes

```csharp
[Trace]
internal static class ListExtensions
{
    extension<T>( IReadOnlyList<T> list )
    {
        public T this[ Index index ] => list[index.GetOffset( list.Count )];
    }
}
```

### What Metalama produces

The override is inlined, so the shape follows the extension property baseline
(`ExtensionMembers_OverrideProperty.t.cs:3-23`), with the receiver parameter substituted for `this`:

```csharp
internal static class ListExtensions
{
  extension<T>(IReadOnlyList<T> list)
  {
    public T this[Index index]
    {
      get
      {
        global::System.Console.WriteLine("Get ListExtensions.extension(IReadOnlyList<T>).this[Index].get");
        global::System.Console.WriteLine(list);
        return list[index.GetOffset(list.Count)];
      }
    }
  }
}
```

### Introducing: the restriction that has to be lifted

The restriction is one imperative check. `AdviceFactory.IntroduceIndexer` calls `ValidateNotExtensionBlock` before
anything else:

```csharp
// Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:1404-1406
this.Validate( targetType, AdviceKind.IntroduceIndexer );

ValidateNotExtensionBlock( targetType, "an indexer" );
```

```csharp
// Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:526-534
private static void ValidateNotExtensionBlock( IDeclaration declaration, string introduced )
{
    if ( declaration.DeclarationKind == DeclarationKind.ExtensionBlock )
    {
        throw new InvalidOperationException(
            MetalamaStringFormatter.Format( $"Cannot introduce {introduced} into '{declaration}' because it represents an extension block." ) );
    }
}
```

The eligibility rule already admits an extension block, so the imperative check is the only barrier:
`_introduceRule` accepts `TypeKind.Extension`
(`Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:117-125`) and
`AdviceKind.IntroduceIndexer` maps to it (`:250-259`). The named restriction to lift is therefore: the call at
`AdviceFactory.cs:1406`, one of the ten call sites of `ValidateNotExtensionBlock`, is removed, and the documentation
summaries at `Metalama.Framework/src/Metalama.Framework/Advising/IAdviceFactory.cs:1039` and `:1061`, which today read
"extension members (methods, properties, operators)", regain the word "indexers".

Two engine pieces are missing behind the check and are not interface design:
`ExtensionImplementationHelper` has no indexer counterpart for the implicit static implementation methods, and
`IntroduceIndexerTransformation` has no `GetImplicitDeclarations` override (LK-6).

### What the author writes to introduce one

No new advice method and no new overload. The existing `IntroduceIndexer` signature is used unchanged
(`IAdviceFactory.cs:482-491` for the single-index form, `:548-557` for the multi-index form), with an extension block
as the target:

```csharp
public sealed class IndexerIntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var block = builder.IntroduceExtensionBlock( typeof(string), "self" ).Declaration;

        builder.With( block ).IntroduceIndexer(
            typeof(int),
            nameof(this.GetTemplate),
            nameof(this.SetTemplate) );
    }

    [Template]
    public char GetTemplate( int index ) => meta.Target.Parameters[0].Value is int i ? 'x' : 'y';

    [Template]
    public void SetTemplate( int index, char value ) { }
}
```

That is the same code as the test that pins the current rejection
(`Tests/Aspects/Introductions/ExtensionBlocks/ErrorIndexerIntoExtensionBlock.cs:14-20`), which is the point: nothing
about the author-facing shape changes. Only the answer changes.

### What the author sees when it fails, today and after

Today, the exception thrown inside `BuildAspect` is converted into `LAMA0041` by
`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/UserCode/UserCodeInvoker.cs:133-140`, and the expected
output of the pinning test records it verbatim:

```
// Error LAMA0041 on `TargetType`: `'Exception of type 'System.InvalidOperationException' thrown while executing
// BuildAspect for aspect [IntroductionAttribute] applied to 'TargetType': Cannot introduce an indexer into
// 'TargetType.extension(string)' because it represents an extension block.`
```

After the restriction is lifted, one rejection has to remain and one has to be added, because the language forbids
them. An extension block that declares an indexer must have a named receiver parameter, since an indexer is always an
instance member; and the `init` accessor and the `abstract`, `virtual`, `override`, `new`, `sealed`, `partial` and
`protected` modifiers are forbidden on an extension member (LK-6, semantics pass). The condition for the receiver is
expressible in the public code model without referencing an engine type, by testing
`IExtensionBlock.ReceiverParameter` for an empty name
(`Metalama.Framework/src/Metalama.Framework/Code/IExtensionBlock.cs:18-21`, the same condition the builder evaluates
at `ExtensionBlockBuilder.cs:34` and the source block at `ExtensionBlockImpl.cs:24`). So:

```
error LAMA0037: The aspect 'IndexerIntroduction' cannot be applied to the extension block
'TargetType.extension(string)' because an indexer requires a named receiver parameter.
```

The shape question here is whether that rule is a dedicated eligibility rule for `AdviceKind.IntroduceIndexer` or a
tightening of `_introduceRule`. It must be dedicated: `_introduceRule` is shared by nine advice kinds
(`EligibilityRuleFactory.cs:250-259`), and a static extension block is a legal target for a method introduction.

### What the author sees when the override cannot be inlined

`LinkerAnalysisStep` reports `LAMA0699` for every non-inlined semantic whose symbol is a property with parameters
(`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.cs:850-906`), which covers extension
indexers and ordinary indexers alike:

```
error LAMA0699: Version of declaration 'ListExtensions.extension(IReadOnlyList<T>).this[Index]' provided by
'TraceAttribute' cannot be inlined. It is not currently possible to generate non-inlined code for this declaration.
```

That is not new behaviour and is tracked by the open issue #937. The adjacent defect that is worth naming here is
outside C# 15: the non-inlined trampolines for extension methods and properties emit `this.<member>`
(`LinkerRewritingDriver.Methods.cs:340-351`, `LinkerRewritingDriver.Properties.cs:668-679`), which is invalid inside
an extension block because an extension member has no `this`. That repairs already shipped C# 14 behaviour and does
not need C# 15.

---

## 4. The forbidden labeled `break` and `continue` in templates

### The draft diagnostic

`TemplatingDiagnosticDescriptors` reserves the ranges 100 to 119 and 220 to 299
(`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingDiagnosticDescriptors.cs:20`). The highest
identifier allocated in the file is `LAMA0293`
(`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingDiagnosticDescriptors.cs:713`). The next free
identifier in the reserved range is therefore `LAMA0294`, and a repository-wide search finds no occurrence of it.

```csharp
// Added to Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingDiagnosticDescriptors.cs,
// after CannotUseNormalTemplateWithTryCatchOnAsyncIterator at :710-719.

internal static readonly DiagnosticDefinition<string>
    LabeledBreakOrContinueNotSupported
        = new(
            "LAMA0294",
            "A labeled 'break' or 'continue' statement is not supported in a template.",
            "The label '{0}' cannot be used in a template. The template compiler must classify every statement as "
            + "run-time or compile-time, and the loop that a label designates may have a different scope than the "
            + "statement that names it. Remove the label and restructure the loops, for instance by setting a "
            + "compile-time or run-time flag and testing it in the outer loop.",
            _category,
            Error );
```

Severity is `Error`, following `goto`, which is the closest existing rejection
(`TemplateAnnotator.cs:2600-2605`). It cannot be a warning: a template with a dropped label produces run-time code
whose `break` targets the innermost loop instead of the labeled one, which is a silent change of control flow
(TP-3 of
[`02-syntax-generator-and-templates.md`](../02-syntax-generator-and-templates.md)).

The location is the label identifier of the jump statement, not the whole statement, so that the editor underlines
the token the author must delete. Reading it must not name `BreakStatementSyntax.Name`: that field is absent from the
Roslyn 5.0 grammar (`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml:1270-1279`) and experimental in the consumed
build, so it is reached through the child nodes of the statement until the latest variant binds against the stable
Roslyn.

The call site is the two existing visitors, which today annotate and do nothing else:

```csharp
// Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:1375-1379, as it stands.
public override SyntaxNode VisitBreakStatement( BreakStatementSyntax node )
    => node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );

public override SyntaxNode VisitContinueStatement( ContinueStatementSyntax node )
    => node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );
```

```csharp
// Draft.
public override SyntaxNode VisitBreakStatement( BreakStatementSyntax node )
{
    this.ReportLabeledJumpIfAny( node );

    return node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );
}

public override SyntaxNode VisitContinueStatement( ContinueStatementSyntax node )
{
    this.ReportLabeledJumpIfAny( node );

    return node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );
}

/// <summary>
/// Reports <see cref="TemplatingDiagnosticDescriptors.LabeledBreakOrContinueNotSupported"/> when a jump statement
/// names a label.
/// </summary>
/// <remarks>
/// The label is read through the child nodes rather than through the strongly typed member, because that member
/// does not exist in the Roslyn version of the lower variant.
/// </remarks>
private void ReportLabeledJumpIfAny( StatementSyntax node )
{
    var label = node.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

    if ( label != null )
    {
        this.ReportDiagnostic(
            TemplatingDiagnosticDescriptors.LabeledBreakOrContinueNotSupported,
            label,
            label.Identifier.ValueText );
    }
}
```

### The alternative shape: reuse `LAMA0101`

`LAMA0101` already exists for exactly this purpose, is parameterized by a feature name, and is what `goto`, `unsafe`
and LINQ report (`TemplatingDiagnosticDescriptors.cs:24-30`, with the helper at `TemplateAnnotator.cs:2590-2591`):

```csharp
public override SyntaxNode VisitBreakStatement( BreakStatementSyntax node )
{
    if ( node.ChildNodes().OfType<IdentifierNameSyntax>().Any() )
    {
        this.ReportUnsupportedLanguageFeature( node.BreakKeyword, "labeled break" );
    }

    return node.AddScopeAnnotation( this._currentScopeContext.CurrentBreakOrContinueScope );
}
```

producing:

```
error LAMA0101: 'labeled break' is not supported in a template.
```

Reusing it costs nothing and is consistent with `goto`. Its weakness is the message: it tells the author that the
feature is unsupported and not why, and the why is the whole point here. The reason is not that the feature is hard
but that the label is unclassifiable, and an author who does not know that will file the rejection as a gap to be
closed rather than as a rule to work with. I would take the dedicated `LAMA0294` for that reason, and I would accept
`LAMA0101` if the discussion prefers not to spend a descriptor.

### What the aspect author writes to trigger it

```csharp
public sealed class ValidateArgumentsAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        outer:
        for ( var attempt = 0; attempt < 3; attempt++ )
        {
            foreach ( var parameter in meta.Target.Parameters )
            {
                if ( parameter.Type.IsNullable == true )
                {
                    continue outer;     // LAMA0294
                }
            }
        }

        return meta.Proceed();
    }
}
```

The outer `for` is run-time, the inner `foreach` over `meta.Target.Parameters` is compile-time, and `continue outer;`
sits in the compile-time loop while naming the run-time one. `VisitContinueStatement` annotates it with
`CurrentBreakOrContinueScope`, which is the scope of the innermost loop, so without the diagnostic the statement is
classified compile-time and the label designates a run-time loop. That is the ambiguity that decision 4 names.

### The message the author sees

```
error LAMA0294: The label 'outer' cannot be used in a template. The template compiler must classify every statement
as run-time or compile-time, and the loop that a label designates may have a different scope than the statement that
names it. Remove the label and restructure the loops, for instance by setting a compile-time or run-time flag and
testing it in the outer loop.
```

reported on the token `outer` of the `continue` statement.

### The neighbouring case that must keep working

Run-time code with a labeled `break`, transformed by an aspect whose template contains no label. What the author's
user writes:

```csharp
public sealed class Finder
{
    [Log]
    public (int Row, int Column) Find( int[,] grid, int needle )
    {
        search:
        for ( var row = 0; row < grid.GetLength( 0 ); row++ )
        {
            for ( var column = 0; column < grid.GetLength( 1 ); column++ )
            {
                if ( grid[row, column] == needle )
                {
                    return (row, column);
                }

                if ( grid[row, column] < 0 )
                {
                    break search;
                }
            }
        }

        goto search;
    }
}
```

What the aspect author writes, which contains no label at all:

```csharp
public sealed class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Entering {meta.Target.Method}" );

        return meta.Proceed();
    }
}
```

What Metalama produces: the user body, label and labeled jump intact, wrapped by the template.

```csharp
public (int Row, int Column) Find(int[,] grid, int needle)
{
  global::System.Console.WriteLine("Entering Finder.Find(int[,], int)");
  search:
  for (var row = 0; row < grid.GetLength(0); row++)
  {
    for (var column = 0; column < grid.GetLength(1); column++)
    {
      if (grid[row, column] == needle)
      {
        return (row, column);
      }

      if (grid[row, column] < 0)
      {
        break search;
      }
    }
  }

  goto search;
}
```

### How the two are told apart in the annotator

They are not told apart by a test inside the annotator. They are told apart by whether the annotator runs at all.

The annotator visits template bodies only. `TemplatingCodeValidator.Visitor` calls the template compiler under the
`IsInTemplate` guard:

```csharp
// Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingCodeValidator.Visitor.cs:541-552
if ( this.IsInTemplate )
{
    if ( this._isDesignTime && !node.IsKind( SyntaxKind.UnknownAccessorDeclaration ) )
    {
        // ...
        _ = this._templateCompiler.TryAnnotate( node, this._semanticModel, this, this._cancellationToken, out _, out _ );
    }
    else
    {
        // The template compiler will be called by the main pipeline.
    }
}
```

`Finder.Find` is not a template, so `TryAnnotate` is never called on it, no scope annotation is ever computed for its
statements, and `LAMA0294` cannot fire on it. Its body reaches the linker as ordinary syntax, which the injection and
linking steps copy. The label survives because nothing renames it: the template compiler reserves unique run-time
names only for the symbol kinds accepted by `IsLocalSymbol`, which does not include a label symbol
(`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompilerRewriter.cs:390-402`, `:491-497`).

That is also the residual risk, and it belongs to the linker rather than to this diagnostic. When two bodies that
each carry a label are merged into one statement list by the inlining substitution, the result is `CS0140` or
`CS0158` (finding LK-9). Forbidding labels in templates removes one of the two sources of collision, the template
label, and leaves the other, a user label copied through an inlining, which the existing linker test
`Tests/Methods/Overrides/TargetBody/UsingLocal_Jump.cs:24` already exhibits. So `LAMA0294` is necessary and not
sufficient, and the discussion should not read it as closing LK-9.

One question the draft leaves open deliberately: whether a plain labeled statement in a template, with no jump
naming it, is also rejected. Decision 4 forbids the labeled jump and says nothing about the label. A label in a
template is legal and useless today, because `goto` is already rejected. Rejecting it as well is one more visitor
override and removes the template half of the collision entirely; keeping it legal preserves a construct nobody
writes. I lean to rejecting it, under the same identifier, because the two rules are then one sentence in the
documentation instead of two.

---

## 5. The divergence on the Roslyn 5.0 variant

### The situation, restated in one paragraph

`Metalama.Framework` is not built per Roslyn version, so `IsUnion` and `IsClosed` exist in every host, while the
engine code that answers them is compiled only in the latest variant (section 6). The Roslyn 5.0 variant serves
Rider and the Visual Studio Code C# Dev Kit
(`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:31-42`, which returns the
`5.10.0` variant for a host at 5.10 or above and the `5.0.0` variant down to the floor of 5.0 at `:23`). There,
`IsUnion` and `IsClosed` return `false`, an aspect sees a union as an ordinary struct, and the editor and the command
line disagree with nothing reporting it.

### Answer A: stay silent

What the aspect author writes is section 1 unchanged. What the author's user writes is the union of section 1. What
the user sees in Rider is the absence of everything the aspect would have produced:

```csharp
// What the build produces, and what Visual Studio 2027 shows.
partial union Shape
{
    public bool VisitCircle() { /* ... */ }
    public bool VisitRectangle() { /* ... */ }
}

// What Rider shows: nothing. The aspect took the SkipAspect branch because IsUnion returned false.
```

No diagnostic anywhere. The user concludes that the aspect does not work, or does not notice, and debugs source that
does not correspond to the assembly.

The argument for staying silent is threefold. First, the user of Rider cannot act on the report except by changing
the integrated development environment, and a warning whose only remedy is to use a different product is the kind
that trains users to suppress the whole category. Second, the condition that a lower variant can detect is a property
of the project and not of the code: it would fire on every project of a solution, including the ones that contain no
union and no closed class, which is most of them. Third, there is a precedent for degrading quietly, and the comment
that records it already anticipates exactly this case:

```csharp
// Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs:166-181
public override LanguageVersion LanguageVersion
{
    get
    {
        var s = this.GetStringOption( MSBuildPropertyNames.LangVersion );

        if ( !LanguageVersionFacts.TryParse( s, out var version ) )
        {
            // This can happen if the property is set to an invalid value, but also if the IDE runs
            // a lower Roslyn version than the one required by the project. In this case, we return 
            // the latest supported version of the current Metalama build, for the current Roslyn version.
            return SupportedCSharpVersions.Latest;
        }

        return version.MapSpecifiedToEffectiveVersion();
    }
}
```

### Answer B: report that the design-time result is incomplete

The descriptor belongs in `DesignTimeDiagnosticDescriptors`, whose reserved range is 300 to 319
(`Metalama.Framework/src/Metalama.Framework.DesignTime/DiagnosticAnalysis/DesignTimeDiagnosticDescriptors.cs:15`).
The allocated identifiers there are `LAMA0301`, `LAMA0302`, `LAMA0303`, `LAMA0304`, `LAMA0306` and `LAMA0307`, so
`LAMA0305` is free and a repository-wide search finds no occurrence of it.

```csharp
/// <summary>
/// Reported when the Roslyn version of the host is below the version required by the language version of the
/// project, so that the design-time result is computed from a code model in which the C# 15 constructs are invisible.
/// </summary>
/// <remarks>
/// <para>
/// Design-time only. The command-line build runs a Roslyn that parses the whole project, so the build is correct and
/// this warning is the only notice that the editor and the build disagree.
/// </para>
/// <para>
/// Reported once per project, on the first syntax tree, following the pattern of
/// <see cref="DuplicateSyntaxTreePath"/>, so that the analyzer invocation Roslyn makes for every file does not
/// produce one warning per file.
/// </para>
/// </remarks>
internal static readonly DiagnosticDefinition<(string HostRoslynVersion, string LanguageVersion)>
    DesignTimeLanguageVersionNotSupportedByHost
        = new(
            "LAMA0305",
            Warning,
            "This editor runs Roslyn {0}, which does not support C# {1}, the language version of this project. "
            + "Metalama analyzes the project with that Roslyn, so a union type and a closed hierarchy are invisible "
            + "to aspects here and the code that Metalama shows may differ from the code it produces at build time. "
            + "The build is not affected. Use an editor based on Roslyn 5.12 or later to see the complete result.",
            "The design-time result is incomplete because the editor runs an older Roslyn.",
            _category );
```

The call site is `TheDiagnosticAnalyzer.AnalyzeSemanticModel`, beside `ReportDuplicateSyntaxTreePath`
(`Metalama.Framework/src/Metalama.Framework.DesignTime/DiagnosticAnalysis/TheDiagnosticAnalyzer.cs:111`, with the
helper at `:398-419`). That is the only design-time surface that reaches the editor with a location, and the
rationale recorded there for `LAMA0307` applies word for word: the condition is a property of the compilation the
integrated development environment built and not of anything the pipeline did.

What the user sees in the Rider error list, once per project:

```
warning LAMA0305: This editor runs Roslyn 5.0, which does not support C# 15, the language version of this project.
Metalama analyzes the project with that Roslyn, so a union type and a closed hierarchy are invisible to aspects here
and the code that Metalama shows may differ from the code it produces at build time. The build is not affected. Use
an editor based on Roslyn 5.12 or later to see the complete result.
```

The argument for reporting is that the failure is not "nothing happens". It is generated code in the editor that
differs from the assembly, which is worse than a warning, because the user reasons about source that the compiler
never saw. Section 6 calls this the same class of failure that the platform baseline document names as the reason
for deriving the Roslyn floor deliberately, and a class of failure that a document singles out as the one to avoid
should not be shipped silent. The cost is one descriptor and one call site, and there is a direct precedent at build
time for refusing to be silent about a host Roslyn that Metalama cannot serve:

```csharp
// Metalama.Framework/src/Metalama.Framework.CompilerExtensions/MetalamaSourceTransformer.cs:17-31
/// <summary>
/// The descriptor of the diagnostic reported when the Roslyn version of the compiler is below the lowest
/// supported one. At compile time, doing nothing would apply no aspect and report nothing, which is worse
/// than failing the build, so this is an error and not a warning.
/// </summary>
private static readonly DiagnosticDescriptor _unsupportedRoslynVersion = new(
    "LAMA0087",
    "The Roslyn version of the compiler is not supported by Metalama",
    "Metalama requires Roslyn {0} or later, but the compiler is running Roslyn {1}, for which this build of "
    + "Metalama embeds no implementation. No aspect has been applied to this project. Upgrade the .NET SDK, "
    + "or use a version of Metalama that supports Roslyn {1}.",
    "Metalama.General",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true );
```

### How the lower variant detects the condition, which decides what is reportable

This is the part of answer B that constrains its wording, and it is where I am least certain.

The lower variant cannot report at the point of use. Recognizing a union requires `ITypeSymbol.IsUnion`, which does
not exist in Roslyn 5.0, so there is no per-declaration trigger available. Whatever is reported is a property of the
project.

Three project-level channels exist, and none is exact.

- The explicit language version. `LangVersion=15.0` fails `LanguageVersionFacts.TryParse` under Roslyn 5.0, which is
  the branch quoted above at `MSBuildProjectOptions.cs:172-178`. This detects an explicit setting and misses the
  common case, because a `net11.0` project usually leaves `LangVersion` implicit or set to `latest`, which parses
  successfully and maps to C# 14 under that Roslyn.
- The .NET SDK version. `LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` already reads the SDK version from
  an MSBuild property and maps it to a language version
  (`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:45-60`, where the mapping
  is `>= 10 => CSharp14` today and would gain `>= 11 => CSharp15`). Comparing that against
  `SupportedCSharpVersions.GetMaxLanguageVersion` of the host Roslyn
  (`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:149-159`) gives a
  version-neutral test that names no Roslyn 5.12 member. It over-reports for a `net10.0` project built with the .NET
  11 SDK and pinned to C# 14.
- The numeric language version. `AllLanguageVersions`
  (`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:12-19`) already exposes
  language versions by numeric cast precisely so that the engine compiles against any Roslyn, so
  `(LanguageVersion) 1500` is expressible in both variants. This is the mechanism, not the detection: the value has
  to come from one of the two channels above, because Roslyn 5.0 never produces it.

The honest combination is the second channel narrowed by the first: report when the SDK implies C# 15 or above and
the effective language version of the project is not explicitly pinned below it, and the host Roslyn supports less.
That is still an over-report for a project that uses no C# 15 construct, which is the strongest argument answer A
has.

### Recommendation

Report, with three constraints on the report.

Report because the alternative ships a known silent divergence between the editor and the build, and because the
repository has already decided the analogous question in the opposite direction at build time, in `LAMA0087`, with a
comment that gives the reason: doing nothing "would apply no aspect and report nothing, which is worse than failing
the build". The design-time case is milder, so the severity is `Warning` and not `Error`, but the reasoning is the
same.

The three constraints follow from the detection problem.

1. Report once per project, on one syntax tree, exactly as `LAMA0307` does
   (`TheDiagnosticAnalyzer.cs:383-396`). One warning per file for a condition that is a property of the project is
   the failure mode that would make the warning hated.
2. Word the message as a statement about the editor, not about the code. The user cannot fix their code; they can
   decide not to trust the editor for this project, and that is the action the message must enable.
3. Give it an opt-out, an MSBuild property in the manner of the existing Metalama options, because a team that has
   deliberately chosen Rider and has no union in the solution should be able to silence it once. Without an opt-out
   the over-reporting of the detection channel makes answer B worse than answer A for that team.

What would settle the residual uncertainty is one measurement that this session could not make: whether Rider's
hosted Roslyn 5.0 is what surfaces C# parse errors to the user, or whether the JetBrains front end parses the file
itself. If Roslyn's parse errors reach the user, then a project whose source contains a `union` declaration already
shows a wall of errors in Rider, the divergence is loud rather than silent for the source case, and answer A becomes
defensible for it, leaving only the metadata case, a union in a referenced assembly, genuinely silent. If they do
not, the source case is silent too and answer B is the only defensible one. That measurement is a fifteen-minute
experiment with Rider 2026.2 and a C# 15 file, and it should be made before the discussion settles this.

---

## Open choices the discussion must settle

1. **The union member set.** Two members on `INamedType` (`IsUnion`, `UnionCaseTypes`) plus extension methods for the
   synthesized `Value` property and the per-case constructors, or a bundle `INamedType.Union` returning an
   `IUnionInfo`. The flat shape follows the `IsRecord` precedent; the bundle avoids a magic string and survives a
   future writer story better. The union introduction analysis owns the writer, and its answer should decide this
   one.
2. **Whether `UnionCaseTypes` ships with `IsUnion` or later.** `ITypeSymbol.UnionCaseTypes` does not exist in the
   consumed Roslyn build, so `IsUnion` can ship first. Shipping half the pair means an aspect can recognize a union
   and cannot enumerate it, which is close to useless; shipping both means the story waits for Roslyn 5.12.
3. **Whether a closed type from a referenced assembly is in scope.** If it is not, `IsClosed` plus documentation is
   the whole of subject 2 and no derived-types member is warranted. If it is, a member built on Roslyn's
   `GetClosedDerivedTypeInfo` is warranted, it must honour the completeness flag of the Roslyn result, and it changes
   the answer of section 2.
4. **Whether extension indexer introduction is one story with the override tests or two.** The override path needs no
   interface change and only tests; the introduction path needs the check at `AdviceFactory.cs:1406` removed, a
   dedicated eligibility rule, an indexer counterpart in `ExtensionImplementationHelper` and a
   `GetImplicitDeclarations` override. They can ship separately.
5. **A dedicated `LAMA0294` or a reuse of `LAMA0101` for the labeled jump.** The dedicated descriptor can explain why
   the construct is unclassifiable; the reuse costs nothing and matches `goto`.
6. **Whether a plain labeled statement in a template is rejected too.** Decision 4 forbids only the labeled jump. A
   bare label is legal and useless today, and rejecting it removes the template half of the inlining label collision
   of LK-9.
7. **Silence or a report on the Roslyn 5.0 variant, and if a report, its trigger.** The trigger cannot be the union
   declaration, because the lower variant cannot see one. The recommendation above is a project-level warning derived
   from the SDK version, reported once per project, with an opt-out. The alternative triggers, and the option of
   silence, are argued in section 5.
8. **Whether the Rider measurement is made before the decision.** Whether Rider surfaces the hosted Roslyn's parse
   errors changes which of the two answers in section 5 is defensible for source-declared unions.
