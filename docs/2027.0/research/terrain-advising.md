# Terrain map: Advising and advice implementation

Subsystem scope:

- `Metalama.Framework/src/Metalama.Framework.Engine/Advising/**` (32 files)
- `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/**` (6 folders, 74 files)
- `Metalama.Framework/src/Metalama.Framework/Advising/**` (the public compile-time surface)

Adjacent files that this subsystem cannot be changed without (they are the real choke points for several of the
questions below, and they are named where relevant):

- `Metalama.Framework/src/Metalama.Framework/Aspects/AdviserExtensions.cs` (public façade over `IAdviceFactory`)
- `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs` and
  `EligibilityRuleFactory.Contracts.cs` (the eligibility rules that `AdviceFactory.Validate` consults)
- `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs` and
  `ModifierCategories.cs` (every `Introduce*Transformation` emits modifiers through it)
- `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/ExtensionBlockBuilder.cs`,
  `ExtensionReceiverParameterBuilder.cs`

`Metalama.Premium` contains no advice implementation. `grep` for `IAdviceFactory` / `Advising` there matches only
four test files under `src/tests/Metalama.Extensions.CodeFixes.UnitTests` and
`src/tests/Metalama.Extensions.Validation.UnitTests`. Nothing in that repository has to change for a language wave
in this subsystem.

---

## 1. Files and types sensitive to the set of C# language constructs

### 1.1 `Advising/AdviceFactory.cs` (2254 lines) — the largest concentration

`internal sealed partial class AdviceFactory : IAdviceFactoryImpl`. It is the single place where the shape of the
C# language is encoded as an API surface: one method per introducible or overridable construct.

| Lines | Member / construct | What is enumerated |
| --- | --- | --- |
| 404-422 | `Validate( IDeclaration, AdviceKind, params IDeclaration[] )` | dispatches to `EligibilityRuleFactory.GetAdviceEligibilityRule( adviceKind )`; the `AdviceKind` enum is the closed world |
| 441-475 | `ValidateTarget` / local `ValidateOneTarget` | line 468 `target.DeclarationKind is not (DeclarationKind.Namespace or DeclarationKind.Compilation)` |
| 477-486 | `ValidateNotExplicitInterfaceImplementation` | closed list `AdviceKind.IntroduceMethod or IntroduceEvent or IntroduceProperty or IntroduceIndexer` |
| 488-525 | `ValidateIntroduceAttributeTarget` | `switch` over `DeclarationKind.Parameter` / `Constructor` / `Field` with a final `throw new InvalidOperationException` |
| 527-534 | `ValidateNotExtensionBlock` | `declaration.DeclarationKind == DeclarationKind.ExtensionBlock` |
| 536-544 | `ValidateNotExtensionBlockReceiver` | `declaration is IParameter { DeclaringMember: null }` |
| 564-698 | `Override( IMethod, … )` | `switch ( targetMethod.MethodKind )`: `EventAdd` (583), `EventRemove` (600), `EventRaise` (617, `throw new NotImplementedException( "Overriding event raise is not implemented." )` at 619), `PropertyGet` (622), `PropertySet` (653) |
| 700-728 | `IntroduceMethod` | — |
| 730-751 | `IntroduceFinalizer` | no extension-block guard (see §5) |
| 753-804 | `IntroduceUnaryOperator` | 763 `kind.GetCategory() != OperatorCategory.Unary`; 769 `!OperatorData.IsUserDefinable( kind )` |
| 806-867 | `IntroduceBinaryOperator` | 818 `kind.GetCategory() != OperatorCategory.Binary`; 824 `IsUserDefinable` |
| 869-915 | `IntroduceConversionOperator` | 880-886 `(isImplicit, isChecked) switch` → `OperatorKind.ImplicitConversion` / `ExplicitConversion` / `CheckedExplicitConversion`, with `(true, true) => throw new ArgumentOutOfRangeException` |
| 945-970 | `IntroduceConstructor` | 957 `ValidateNotExtensionBlock( targetType, "a constructor" )` |
| 1015-1098 | `OverrideAccessors( IFieldOrPropertyOrIndexer, … )` | 1036-1049 and 1072-1096 `switch` over `DeclarationKind.Field or Property` / `Indexer`, `default: throw new AssertionFailedException` |
| 1116-1191 | `IntroduceField` ×3 | 1128 and 1158 `ValidateNotExtensionBlock( targetType, "a field" )` |
| 1193-1239 | `IntroduceAutomaticProperty` ×2 | 1206 `ValidateNotExtensionBlock( targetType, "an automatic property" )` |
| 1241-1316 | `IntroduceProperty` ×2 | deliberately no extension-block guard (properties are legal in an extension block) |
| 1318-1426 | `IntroduceIndexer` ×4 (three delegate to the fourth) | **1406 `ValidateNotExtensionBlock( targetType, "an indexer" )`** — the single most important C# 15 hotspot in this subsystem |
| 1428-1476 | `OverrideAccessors( IEvent, … )` | 1443 `throw new NotImplementedException( "Using raiseTemplate is not currently supported." )` |
| 1478-1511 | `IntroduceEvent( name )` | 1490 `ValidateNotExtensionBlock( targetType, "an event" )` |
| 1513-1562 | `IntroduceEvent( name, addTemplate, removeTemplate, … )` | 1530 the same `raiseTemplate` `NotImplementedException`; **no** `ValidateNotExtensionBlock` (see §5) |
| 1871-1887 | `IntroduceAttribute` | 1879 `ValidateNotExtensionBlock( targetDeclaration, "an attribute" )`; 1880 `ValidateNotExtensionBlockReceiver` |
| 2050-2071 | `IntroduceClass` | 2060 guard; hard-codes `TypeKind.Class` at 2068 |
| 2073-2093 | `IntroduceInterface` | 2083 guard; hard-codes `TypeKind.Interface` at 2090 |
| 2095-2126 | `IntroduceExtensionBlock` ×2 | 2106 `ValidateNotExtensionBlock( targetStaticClass, "an extension block" )` |
| 2172-2180 | `RequireAspect` | 2176 `this.Target.DeclarationKind is DeclarationKind.Compilation or DeclarationKind.Namespace` |

Exact text of the two extension-block gates (lines 527-544):

```csharp
private static void ValidateNotExtensionBlock( IDeclaration declaration, string introduced )
{
    if ( declaration.DeclarationKind == DeclarationKind.ExtensionBlock )
    {
        throw new InvalidOperationException(
            MetalamaStringFormatter.Format( $"Cannot introduce {introduced} into '{declaration}' because it represents an extension block." ) );
    }
}

private static void ValidateNotExtensionBlockReceiver( IDeclaration declaration, string introduced )
{
    if ( declaration.DeclarationKind == DeclarationKind.Parameter && declaration is IParameter { DeclaringMember: null } )
    { … }
}
```

There is exactly one call site per construct; the whole "which constructs may live inside an `extension` block"
policy is these eleven call sites plus `IntroduceMemberAdvice.ValidateBuilder` line 197.

### 1.2 `Advising/TemplateBindingHelper.cs`

- 103-121 `expectedParameterCount` switch over `OperatorCategory`, including the C# 14 arms:
  `OperatorCategory.UnaryAssignment => 0,  // C# 14 compound operators like ++` (line 119) and
  `OperatorCategory.BinaryAssignment => 1, // C# 14 compound operators like +=` (line 120), with
  `_ => throw new AssertionFailedException( $"Invalid value for OperatorCategory: …" )`.
- 443-501 switch over `OurMethodKind.PropertyGet / PropertySet / EventAdd / EventRemove / EventRaise` computing the
  expected run-time parameter count of an accessor template; lines 452-461 special-case
  `ContainingDeclaration: { DeclarationKind: DeclarationKind.Indexer } and IIndexer { Parameters.Count: … }`.
- 770-805 type-mapping switch over `TypeKind.TypeParameter`, `TypeKind.Dynamic`, `fromType.TypeKind.IsNamedType`.

### 1.3 `Advising/TemplateMember.cs` — the C# 14 `field` keyword detection

- 61-70: `public bool IntroducesBackingField { get; }` and `public bool IsBackingFieldAssigned { get; }`, both
  documented as "Gets a value indicating whether the property template uses the C# 14 `field` keyword".
- 160-165: guard that an accessor template `MethodKind: PropertySet or EventAdd or EventRemove` has exactly one
  parameter.
- 184-236: reads the two flags from `CompiledTemplateAttribute` on the getter and the setter, and from the
  accessor symbol when the template is an accessor (`MethodKind: PropertyGet or PropertySet`).
- 243-313: `GetCompiledTemplateAttribute`. Lines 293-310 are the same-project fallback:

```csharp
attribute.IntroducesBackingField = declaration.DeclaringSyntaxReferences
    .Any( syntaxRef => syntaxRef.GetSyntax() is { SyntaxKind.IsAccessorDeclaration: true } and AccessorDeclarationSyntax accessor
                       && SyntaxHelpers.ContainsFieldExpression( accessor ) );
```

  `SyntaxHelpers.ContainsFieldExpression` / `ContainsFieldAssignment` live in
  `Utilities/Roslyn/SyntaxHelpers.cs` lines 93-140 and walk for `FieldExpressionSyntax`. `SyntaxKind.IsAccessorDeclaration`
  is the extension property in `Utilities/Roslyn/SyntaxKindExtensions.cs` line 69.

### 1.4 `Advising/TemplateExtensions.cs`

- 16-59 `GetAccessorTemplates( TemplateMember<IProperty>? )`: line 32
  `if ( propertyKind != PropertyKind.Auto || propertyTemplate.IntroducesBackingField )`, with the C# 14 comment at
  lines 23-31 explaining semi-auto properties.
- 61-94 the event-accessor equivalent, keyed on `IsEventField()`.
- 96-119 `TemplateKind` switches (`Async`, `IAsyncEnumerable`, `IAsyncEnumerator`) — language-shape sensitive but not
  syntax-node sensitive.

### 1.5 `Advising/DeclarationExtensions.cs`

Lines 16-20: a `(DeclarationKind, DeclarationKind)` tuple switch pairing `Method/Method`, `Indexer/Indexer`,
`Field/Field`, `Property/Property`, `Event/Event`. Any new member kind that can be introduced must be added here or
`SignatureEquals` silently returns the fall-through value.

### 1.6 `AdviceImpl/AdviceSyntaxGenerator.cs`

Lines 40-63 `GetAttributeLists`: a `switch ( declaration.DeclarationKind )` with only two arms
(`Method` at 42, `Property … { IsAutoPropertyOrField: true }` at 60 carrying `// TODO: field-level attributes`) and
**no default arm**. Line 49 enumerates `MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.PropertySet` to
decide whether to emit `[param:]` attributes.

### 1.7 `AdviceImpl/Introduction/IntroduceMethodTransformation.cs`

- 46-224 `switch ( finalMethod.MethodKind )`: `MethodKind.Finalizer` (49), `MethodKind.Operator` (64), default
  method (around 205). The operator arm branches on `operatorData.Category == OperatorCategory.Conversion` (67) and
  `operatorData.IsChecked` (86, 116) and asserts at line 97
  `Invariant.Assert( finalMethod.Parameters.Count is 0 or 1 or 2 )` with the comment
  `// 0 params for unary assignment operators (++, --), 1 for unary/binary assignment, 2 for binary operators.`
- 45 `var hasNoBody = finalMethod.IsAbstract || finalMethod.IsPartial || finalMethod.IsExtern;` — the modifier set
  is enumerated by hand.
- 227-266 `GetImplicitDeclarations`: 233 `if ( containingDeclaration is not IExtensionBlock extensionBlock ) return [];`,
  then 240-245 `OperatorKind != OperatorKind.None ? … MemberName : Name` and
  `? MethodKind.Operator : MethodKind.Default`.

### 1.8 `AdviceImpl/Introduction/IntroducePropertyTransformation.cs`

- 60 `finalProperty is { IsAutoPropertyOrField: true, DeclaringType.TypeKind: TypeKind.Struct }`.
- 217 `AttributeTargetSpecifier( Token( SyntaxKind.FieldKeyword ) )`.
- 225-277 `GetImplicitDeclarations`: 231 the same `is not IExtensionBlock` early return, then one
  `ExtensionImplementationHelper.CreateImplicitAccessorMethod` call per existing accessor.

### 1.9 `AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs`

Lines 62-92, the closed world of introducible type declarations:

```csharp
var type = (this.BuilderData.TypeKind switch
{
    TypeKind.Class     => (TypeDeclarationSyntax) ClassDeclaration( … ),
    TypeKind.Struct    => StructDeclaration( … ),
    TypeKind.Interface => InterfaceDeclaration( … ),
    _ => throw new AssertionFailedException( $"Unsupported type kind '{introducedType.TypeKind}'." )
})
```

Lines 45-59 also enumerate `VarianceKind.In` / `VarianceKind.Out`. Lines 94-124 switch over the containing
`DeclarationKind.NamedType` / `Namespace`, with `default: throw new AssertionFailedException`.

### 1.10 `AdviceImpl/Introduction/IntroduceExtensionBlockTransformation.cs`

Lines 56-66 call the Roslyn factory `ExtensionBlockDeclaration( … Token( SyntaxKind.ExtensionKeyword ) … )` and pass
`default` for the modifier list with the comment `// Modifiers (extension blocks don't have modifiers).` This is the
template that a hypothetical `UnionDeclaration` introduction would copy.

### 1.11 `AdviceImpl/Introduction/ExtensionImplementationHelper.cs`

The mapping from an extension member to the static implementation method the compiler would emit.

- 38-131 `CreateImplicitMethod( …, MethodKind methodKind = MethodKind.Default, OperatorKind operatorKind = OperatorKind.None, … )`.
- 89-98 `if ( !isSourceMemberStatic ) { … AddParameter( receiverParam.Name, receiverParam.Type, receiverParam.RefKind ); }`.
- 163-…, `CreateImplicitAccessorMethod`, line 177: `var methodName = (isSetter ? "set_" : "get_") + propertyName;`
  It takes a single `propertyType` and no index parameters, so it cannot express an extension **indexer**.

### 1.12 `AdviceImpl/Introduction/IntroduceMemberAdvice.cs`

- 88 `var isInterfaceMember = this.TargetDeclaration.TypeKind is TypeKind.Interface;`
- 91-134: the modifier computation, enumerating `IsAbstract`, `IsPartial`, `IsExtern`, `IsSealed`, `IsVirtual`.
- 145-151 `builder.IsStatic = this._scope switch { IntroductionScope.Default / Instance / Static, _ => throw }`.
- **197**: `if ( targetDeclaration.IsStatic && !builder.IsStatic && targetDeclaration.TypeKind != TypeKind.Extension )`
  with the comment `// Skip this check for extension blocks, which can contain instance-appearing members that become extension methods.`
- 253-306 `SetBuilderExplicitInterfaceImplementation`: a `switch ( builder )` over
  `MethodBuilder` / `PropertyBuilder` / `EventBuilder` / `IndexerBuilder`.

### 1.13 `AdviceImpl/Contracts/**`

- `ParameterContractAdvice.cs` 25-77: the dispatch by the **containing declaration** of the parameter:
  `IExtensionBlock` (27) → `ContractExtensionBlockTransformation`, `IIndexer` (39), `IMethod` (51),
  `IConstructor` (63), `default: throw new AssertionFailedException()` (75-76).
- `FieldOrPropertyOrIndexerContractAdvice.cs` 29-58: `DeclarationKind.Property` / `Field` (two arms) / `Indexer`,
  `default: throw new AssertionFailedException()`.
- **`ContractExtensionBlockTransformation.cs`** 45-107 `GetInsertedStatements`. The member fan-out:

```csharp
foreach ( var method in extensionBlock.Methods.Where( m => !m.IsStatic ) )      // line 75
foreach ( var property in extensionBlock.Properties.Where( p => !p.IsStatic ) ) // line 80
foreach ( var indexer in extensionBlock.Indexers.Where( i => !i.IsStatic ) )    // line 93
```

  The `Indexers` loop (93-104) has been present since the first commit of the file (30e21aea98, #1127) even though
  `IntroduceIndexer` refuses extension blocks and C# 14 source cannot declare one. It is speculative support that
  becomes live the moment C# 15 permits extension indexers. There is **no** loop over events, fields, constructors
  or nested types.
  Lines 59-69 map `RefKind.Ref → ContractDirection.Both`, `RefKind.Out → Output`, `_ → Input`.
- `ContractBaseTransformation.cs` 114-121: `DeclarationKind.Parameter when … IsReturnParameter` /
  `DeclarationKind.Parameter when … param` / `_ => $"unexpected declaration '{target}'"` (a display-string
  fall-through, not a throw).
- `ContractIndexerTransformation.cs` 47 (`case DeclarationKind.Indexer:`), 122
  (`case DeclarationKind.Parameter when targetDeclaration is IParameter parameter:`), 193.
- `ContractMethodTransformation.cs` 47 (return parameter), 68 (ordinary parameter).
- `ContractConstructorTransformation.cs` 47.

### 1.14 `AdviceImpl/InterfaceImplementation/ImplementInterfaceAdvice.cs` (≈1050 lines)

- 105 and 362 `this.TargetDeclaration is { TypeKind: TypeKind.Interface }`.
- 154-155 `interfaceMember.DeclarationKind is DeclarationKind.Property or DeclarationKind.Indexer && interfaceMember is IPropertyOrIndexer property`.
- 184-211: the enumeration of interface members to bind, with the explicit gap at line 196:
  `// Indexers are ignored, because there are no indexer templates.` Only `Methods` (184), `Properties` (197) and
  `Events` (206) are walked.
- 373 `case DeclarationKind.Method`, 517 `case DeclarationKind.Property`,
  **779-780 `case DeclarationKind.Indexer: throw new NotImplementedException( "Implementing interface indexers is not yet supported." );`**,
  782 `case DeclarationKind.Event`, 949-950 `default: throw new AssertionFailedException( $"Unexpected kind of declaration: '{interfaceMember}'." )`.
- 608 and 869 `throw new NotImplementedException( $"The strategy OverrideStrategy.{this._overrideStrategy} is not implemented." )`.

### 1.15 `AdviceImpl/Override/**`

- `OverrideHelper.cs` 33-40: the `field`-keyword flags; 49-54 `switch` over `DeclarationKind.Field` /
  `Property` with `_ => null`; 61-134 the same three arms plus
  `default: throw new AssertionFailedException( $"Unexpected declaration: '{targetDeclaration}'." )` at 131-132;
  143-171 `ComputeBackingFieldName` (its collision check `HasMemberWithName` enumerates
  `AllFields`, `AllProperties`, `AllEvents`, `AllMethods` — **not** `AllIndexers` or `AllTypes`);
  187-207 `IntroduceBackingField`; 209-223 `AddTransformationsForStructField` (`type.TypeKind is TypeKind.Struct`).
- `OverrideMethodBaseTransformation.cs` 113-131: `MethodKind.Default or MethodKind.ExplicitInterfaceImplementation`,
  `MethodKind.Finalizer`, `MethodKind.Operator`, `_ => throw new AssertionFailedException( $"Unsupported method kind: …" )`.
- `OverrideEventTransformation.cs` 218-219 (`MethodKind.EventAdd` / `EventRemove`), 260 (`MethodKind.EventRaise`).
- `OverridePropertyBaseTransformation.cs` 108-113 and `OverrideIndexerBaseTransformation.cs` 109-114
  (`MethodKind.PropertyGet` / `PropertySet`).
- `OverridePropertyTransformation.cs` 29-38: `BackingFieldName` and `IntroducesBackingField`, documented as
  "the backing field introduced for a template that uses the C# 14 `field` keyword".

### 1.16 Public surface: `Metalama.Framework/src/Metalama.Framework/Advising/**`

- `AdviceKind.cs` lines 18-51: the 29-value enum. `IntroduceOperator` is `[Obsolete]` (31-32), `IntroduceExtensionBlock`
  is last (50). Every new advice kind lands here and in
  `EligibilityRuleFactory.GetAdviceEligibilityRule` (lines 242-266).
- `IAdviceFactory.cs` (1108 lines): 54 members, one per language construct. `IntroduceIndexer` ×4 at 482, 515, 548,
  581; `IntroduceClass` 1015; `IntroduceInterface` 1031; `IntroduceExtensionBlock` ×2 at 1053 and 1068.
  There is no `IntroduceStruct`, `IntroduceRecord`, `IntroduceEnum`, `IntroduceDelegate` or `IntroduceUnion`.
- `InitializerKind.cs`, `InitializerPosition.cs`, `ConstructorOverloadingActionKind.cs`, `PullActionKind.cs`,
  `AdviceOutcome.cs`, `InterfaceMemberImplementationOutcome.cs`: small closed enums, each switched on somewhere in
  `AdviceImpl`.
- `MethodTemplateSelector.cs` / `GetterTemplateSelector.cs`: enumerate the async / iterator / enumerable template
  variants — that is, a closed list of method body forms the language supports.

### 1.17 Diagnostics

`Advising/AdviceDiagnosticDescriptors.cs`. Language-shape diagnostics: `CannotIntroduceInstanceMember` (LAMA?, line 61),
`CannotIntroduceStaticVirtualMember` (77), `CannotIntroduceStaticSealedMember` (85),
`CannotIntroduceIndexerWithoutParameters` (216), `CannotIntroduceStaticIndexer` (224),
`CannotIntroduceFieldToInterface` (280), `CannotIntroducePartialMemberToNonPartialType` (288),
and the extension-block sub-range 540-549 at lines 302-319:
`ExtensionBlockTargetMustBeStaticClass` = `LAMA0540` (305) and
`CannotIntroduceExtensionBlockIntoExtensionBlock` = `LAMA0541` (313).
The range registry is `Metalama.Framework.Engine/Diagnostics/Ranges.md` line 14 (`| 0540-0549 | Extension Block Introduction`).

---

## 2. Files and types sensitive to the runtime, SDK, Roslyn or IDE version

The honest summary: **this subsystem has almost no direct version sensitivity.** It is target-framework-agnostic
`netstandard2.0` code that consumes the Roslyn syntax API through `Microsoft.CodeAnalysis.CSharp.SyntaxFactory`.
There is not a single `#if` on a Roslyn constant, not a single `LanguageVersion` reference, and not a single
`RuntimeInformation` or `Environment.Version` reference anywhere under `Advising/` or `AdviceImpl/`.

What sensitivity exists is indirect, through the Roslyn API surface it calls:

1. **Syntax-factory calls that only exist from a given Roslyn version.**
   - `Advising/` uses almost none: `TemplateBindingHelper.cs:545` (`SyntaxKind.SimpleMemberAccessExpression`) and
     `TemplateMember.cs:299,308` (`AccessorDeclarationSyntax`, `SyntaxKind.IsAccessorDeclaration`).
   - `AdviceImpl/` has 131 `SyntaxKind.` references. The version-recent ones are
     `SyntaxKind.ExtensionKeyword` and the factory `ExtensionBlockDeclaration(…)`
     (`AdviceImpl/Introduction/IntroduceExtensionBlockTransformation.cs:56-66`), and
     `SyntaxKind.FieldKeyword` (`IntroduceEventTransformation.cs:201`, `IntroducePropertyTransformation.cs:217`).
     Both are satisfied by the Roslyn 5.0 floor, so no branch is needed today.
   - `WithCheckedKeyword` on `OperatorDeclarationSyntax` / `ConversionOperatorDeclarationSyntax`
     (`IntroduceMethodTransformation.cs:86,116`).

2. **The Roslyn-variant mechanism itself.** `eng/RoslynVersions/Roslyn.5.10.0.props` line 10 defines
   `ROSLYN_5_10_0_OR_GREATER`, and `Roslyn.5.0.0.props` lines 8-10 state explicitly:
   "This variant defines no constant. No production source branches on the variant." `updating-roslyn.md` step 12
   repeats: "The variant props files currently define `ROSLYN_5_10_0_OR_GREATER`, which only the aspect tests use."
   Consequently, **the first C# 15 syntax node this subsystem has to emit will be the first production `#if`
   in it**, or will require the latest-variant-only project split. There is no precedent to copy inside
   `Advising/` or `AdviceImpl/`.

3. **`Metalama.Framework.Engine.Analyzers/KindCheckOptimizationAnalyzer.cs`** (lines 626, 689, 723, 745, 772)
   recognises the `{ SyntaxKind.IsAccessorDeclaration: true }` / `DeclarationKind.IsMember` extension-property
   idiom that `Advising/TemplateMember.cs` uses. Its hard-coded list of recognised names at line 723 is a build-time
   coupling: a new `SyntaxKindExtensions` property added for a C# 15 node must be added there too, or the analyzer
   will not optimise (and, under `ContinuousIntegrationBuild=True`, may warn on) the new checks.

4. **`ExtensionBlockDeclarationSyntax` handling in the linker** (`Linking/LinkerInjectionStep.Rewriter.cs`, added by
   commit `f374fce480`, #1159) is the downstream consumer of `IntroduceExtensionBlockTransformation`. It is outside
   this subsystem but is the paired change for anything new emitted here.

5. **No IDE-host sensitivity at all** in this subsystem. The design-time / compile-time distinction reaches it only
   through `context.SyntaxGenerationContext.IsPartial`
   (`AdviceImpl/AdviceSyntaxGenerator.cs:121-131`, "At design time when generating the partial code for source
   generators, we do not expand templates") and through `AdviceFactoryState.ExecutionScenario`
   (`Advising/AdviceFactoryState.cs:63`).

---

## 3. How the subsystem absorbed C# 14

Six distinguishable work items landed in this subsystem. They followed one repeatable shape.

### 3.1 The overall shape

1. **A blanket refusal first.** The original code refused to advise anything inside an extension block, in
   `AdviceFactory.ValidateTarget.ValidateOneTarget`. Commit `737e0347a9` (#1035, "Method overrides and properties
   are working") deleted it:

   ```
   -            // Check that we are not advising extension blocks.
   -            var namedType = target.GetClosestNamedType();
   -            if ( namedType is { TypeKind: TypeKind.Extension } )
   -            { throw new InvalidOperationException( … "is contained in an extension block." ); }
   ```

2. **Replaced by narrow, per-advice refusals.** The blanket check became one `ValidateNotExtensionBlock` call per
   `Introduce*` method that C# 14 does not allow inside an extension block. Commit `5e65ceb149` (#1159) is the
   canonical example and is a two-line diff:

   ```
   +            ValidateNotExtensionBlock( targetType, "an indexer" );
   ```

   with the commit message "C# 14 extension blocks don't support indexers with the this[] syntax (CS9282)."
   Commit `0b31fd8fb1` (#1159) added the same two lines for `IntroduceField`.

3. **Each refusal is pinned by an error aspect test** under
   `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Introductions/ExtensionBlocks/`:
   `ErrorIndexerIntoExtensionBlock.cs` + `.t.cs`, `ErrorFieldIntoExtensionBlock`, `ErrorEventIntoExtensionBlock`,
   `ErrorConstructorIntoExtensionBlock`, `ErrorAutoPropertyIntoExtensionBlock`, `ErrorNestedTypeIntoExtensionBlock`,
   `ErrorExtensionBlockIntoExtensionBlock`, plus target-shape errors (`ErrorTargetIsInterface`,
   `ErrorTargetIsStruct`, `ErrorTargetIsNestedClass`, `ErrorTargetNotStaticClass`) and builder-restriction errors
   (`ErrorSetAccessibility`, `ErrorSetBaseType`, `ErrorSetIsStatic`, `ErrorReceiverDefaultValue`). The expected
   output is the raw `LAMA0041` wrapper around the `InvalidOperationException` message, so the message text is
   part of the test baseline.

4. **When support lands, the refusal is deleted and a fan-out is added.** Commit `30e21aea98` (#1127, "Enable
   contracts on receiver parameters of extension blocks") removed
   `ValidateNotExtensionBlockReceiver( targetParameter, "a contract" )` from `AdviceFactory.AddContract`
   and added, in the same commit, `AdviceImpl/Contracts/ContractExtensionBlockTransformation.cs` (145 lines)
   plus the `case DeclarationKind.Parameter when … IExtensionBlock` arm in `ParameterContractAdvice.cs`
   plus a new eligibility clause in `EligibilityRuleFactory.Contracts.cs`
   (`static bool IsReceiverParameter( IParameter p ) => p.ContainingDeclaration is IExtensionBlock;`, line 111)
   plus seven new aspect tests (`ExtensionMembers_Contract_OnReceiver*`).

5. **New syntax kinds get an implicit-declaration hook, not a special case.** #1036 (commit `22697b6ba5`,
   "Add invoker support for extension member implementation methods") introduced
   `ExtensionImplementationHelper.cs` (190 lines) and overrode `GetImplicitDeclarations()` in
   `IntroduceMethodTransformation` (+39 lines) and `IntroducePropertyTransformation` (+52 lines), wiring them into
   `AdviceFactoryState.AddTransformations` (+6 lines, now lines 75-80):

   ```csharp
   // Add implicit declarations (e.g., implicit static methods for extension block members) to the code model.
   foreach ( var implicitDeclaration in transformation.GetImplicitDeclarations() )
   {
       this.MutableCompilation.AddDeclaration( implicitDeclaration );
   }
   ```

   The virtual `BaseTransformation.GetImplicitDeclarations()` (line 63) returns empty, so a transformation that
   forgets to override it simply produces nothing.

6. **Behaviour that depends on a new construct is carried on the template, not inferred at the use site.**
   #1114 (the `field` keyword) added two booleans to `TemplateMember`, `IntroducesBackingField` and
   `IsBackingFieldAssigned` (commits `aea7b2e5a2`, `929d055d85`, `81e5a5fed7`), read from `CompiledTemplateAttribute`
   for cross-project templates and from `DeclaringSyntaxReferences` for same-project templates, and then consumed at
   exactly two sites: `OverrideHelper.OverrideProperty` (33-40) and `IntroducePropertyAdvice` (62, 289-316).
   Follow-up commits then hardened the derived machinery: `df4ae55b09` (initializer transfer),
   `2c1eb66123` (overflow check in the name loop), `e3b3fc5959` (method-name collision checking).

7. **New operator forms are absorbed by widening a category enum and delegating to a data table.**
   #1116 (commit `5b121f3c21`) replaced `finalMethod.OperatorKind.ToOperatorKeyword()` with
   `OperatorData.GetByKind( finalMethod.OperatorKind )` in `IntroduceMethodTransformation`, added
   `operatorData.IsChecked` handling, and added the `!OperatorData.IsUserDefinable( kind )` gate to
   `IntroduceUnaryOperator` / `IntroduceBinaryOperator` in `AdviceFactory`. The two new categories,
   `OperatorCategory.UnaryAssignment` and `BinaryAssignment`, were added to the enum
   (`Metalama.Framework/Code/OperatorCategory.cs:23-24`) and to `TemplateBindingHelper`'s
   `expectedParameterCount` switch (lines 119-120). Note the resulting asymmetry: overriding a compound assignment
   operator works (tests under `Tests/Aspects/CSharp14/CompoundAssignmentOperator/`), but there is no
   `IntroduceCompoundOperator` API and `IntroduceUnaryOperator` / `IntroduceBinaryOperator` reject those categories.

8. **Partial members were absorbed by an existing boolean.** #1110-#1113 (commit `787ec4fcd8`) only touched
   `IntroduceConstructorAdvice.cs` (+22/-4) and `IntroduceEventTransformation.cs` (+2/-1) in this subsystem,
   because `IsPartial` already existed on the builders and `hasNoBody` already accounted for it.

9. **Tests are foldered by language version.**
   `Tests/Aspects/CSharp14/` has one folder per feature: `CompoundAssignmentOperator`, `ExtensionMembers`,
   `FieldKeyword`, `NullConditionalAssignment`, `PartialConstructor`, `PartialEvent`, `SimpleLambdaModifier`.
   Introduction-side extension-block tests live separately under `Tests/Aspects/Introductions/ExtensionBlocks/`.
   Design-time variants carry `.0.i.cs` / `.1.i.cs` companions
   (`ExtensionMembers_Introduce_DesignTime`, `DesignTime/IntroduceExtensionBlock`).

10. **Diagnostics get a reserved sub-range recorded in `Ranges.md`.** #1159 claimed 0540-0549 for extension block
    introduction and recorded it in `Metalama.Framework.Engine/Diagnostics/Ranges.md` line 14 in the same pull
    request (commit `f776fd9af9`).

### 3.2 What the C# 15 work will therefore look like in this subsystem

- Extension-block **indexers**: delete `AdviceFactory.cs:1406`, delete
  `Tests/Aspects/Introductions/ExtensionBlocks/ErrorIndexerIntoExtensionBlock.{cs,t.cs}` and replace it with a
  positive `IntroduceIndexerIntoExtensionBlock` test, add `IntroduceIndexerTransformation.GetImplicitDeclarations`,
  extend `ExtensionImplementationHelper` with an indexer-accessor overload (`get_Item` / `set_Item` plus the index
  parameters), and add a positive `ExtensionMembers_Contract_OnReceiver_Indexer` test that exercises the already
  present loop at `ContractExtensionBlockTransformation.cs:93`.
- The `closed` modifier: a new `ModifierCategories` flag and a `ModifierHelper` arm, a new builder property, and a
  new `IntroduceMemberAdvice.ValidateBuilder` rule.
- `union`: a new `TypeKind`, a new arm in `IntroduceNamedTypeTransformation.cs:62-92`, a new
  `IAdviceFactory.IntroduceUnion` plus `AdviserExtensions` forwarder, a new `_introduceRule` allowance, and the
  first `#if ROSLYN_5_10_0_OR_GREATER` in production code here.
- `unsafe(expr)`, `with(...)` elements, labelled `break`/`continue`: no advice-surface change; they are template-body
  concerns handled in `Templating/` and `Linking/`, not here.

---

## 4. Extension points that must change per kind of new language construct

### 4.1 A NEW kind of type declaration (for example `union`)

| Order | File | What |
| --- | --- | --- |
| 1 | `Metalama.Framework/Code/TypeKind.cs` | new enum member |
| 2 | `Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79` | new arm in the Roslyn→Metalama `TypeKind` switch (today `_ => throw new InvalidOperationException`) |
| 3 | `Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:47`, `:89-92`, `:121`, `:141`, `:170-172` | the four hard-coded `TypeKind.Class or TypeKind.Struct or TypeKind.Interface (or TypeKind.Extension)` lists |
| 4 | `Metalama.Framework/Advising/IAdviceFactory.cs` (near 1015-1046) | new `IntroduceXxx` method, matching `IntroduceClass` |
| 5 | `Metalama.Framework/Aspects/AdviserExtensions.cs` (near 1702-1740) | the public forwarder |
| 6 | `Metalama.Framework.Engine/Advising/AdviceFactory.cs` (near 2050-2093) | implementation, `ValidateNotExtensionBlock`, and the `TypeKind` literal passed to `IntroduceNamedTypeAdvice` |
| 7 | `AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:62-92` | new syntax-factory arm |
| 8 | `AdviceImpl/Introduction/IntroduceNamedTypeAdvice.cs:108` | `IntroduceImplicitConstructorIfNeeded` (`builder is { TypeKind: TypeKind.Class, IsStatic: false }`) |
| 9 | `CodeModel/Helpers/ModifierHelper.cs:205-240` | the named-type modifier path (line 224 `namedType.IsAbstract && namedType.TypeKind != TypeKind.Interface`) |
| 10 | `Metalama.Framework.Engine/Diagnostics/Ranges.md` | a diagnostic sub-range if new errors are needed |
| 11 | new folder under `Tests/Aspects/Introductions/` and, if the construct is C# 15, `Tests/Aspects/CSharp15/` | tests |

If the new declaration can also **contain** members, add one `ValidateNotXxx` gate per `Introduce*` method it does
not admit, mirroring the eleven `ValidateNotExtensionBlock` call sites, plus an error aspect test per gate.

### 4.2 A NEW modifier (for example `closed`)

| File | What |
| --- | --- |
| `CodeModel/Helpers/ModifierCategories.cs:12-23` | a new flag, and add it to `All` |
| `CodeModel/Helpers/ModifierHelper.cs:80-195` (members) and `:205-240` (types) | the `AddToken( SyntaxKind.XxxKeyword )` arm |
| `CodeModel/Introductions/Builders/MemberBuilder.cs` / `NamedTypeBuilder.cs` | a settable `IsXxx` |
| `AdviceImpl/Introduction/IntroduceMemberAdvice.cs:91-134` | derive the flag from the template and from `TemplateAttributeProperties` |
| `AdviceImpl/Introduction/IntroduceMemberAdvice.cs:168-244` | a `ValidateBuilder` rule and, if it can conflict, a new `AdviceDiagnosticDescriptors` entry (pattern: `CannotIntroducePartialMemberToNonPartialType`, line 288) |
| `AdviceImpl/Introduction/IntroduceMethodTransformation.cs:45` | `hasNoBody` if the modifier implies a bodyless member |
| `Metalama.Framework/Advising/TemplateAttributeProperties.cs` and `ITemplateAttribute` | to let a template declare it |
| `AdviceImpl/Override/*BaseTransformation.cs` | the `GetSyntaxModifierList( ModifierCategories… )` masks at `OverrideConstructorTransformation.cs:79`, `OverrideEventTransformation.cs:119`, `OverrideFinalizerTransformation.cs:79`, `OverrideIndexerBaseTransformation.cs:50`, `OverrideMethodBaseTransformation.cs:51`, `OverridePropertyBaseTransformation.cs:59` — each must decide whether the modifier propagates to the override |

The last row is the easiest to miss: those six masks are explicit allow-lists, so a new modifier is silently dropped
from every generated override until each is revisited.

### 4.3 A NEW expression form (for example `unsafe(expr)`)

Nothing in `Advising/` or `AdviceImpl/` constructs arbitrary user expressions; they build declaration syntax and
delegate bodies to the template engine. The two touch points are:

- `Advising/TemplateMember.cs:293-310` and `Utilities/Roslyn/SyntaxHelpers.cs:93-140` — if the new expression form
  can appear inside a property accessor and changes what the accessor means (as `field` did), the same
  "detect in syntax, record on `CompiledTemplateAttribute`, read back on `TemplateMember`" pattern applies.
- `AdviceImpl/AdviceSyntaxGenerator.cs:100-209` `GetInitializerExpressionOrMethod` — line 172
  `if ( initializerBlock.Statements is [ReturnStatementSyntax { Expression: not null } returnStatement] )` collapses
  a single-return template to an expression. A new expression form does not change this, but a new *statement* form
  that can substitute for `return` would.

Everything else is `Templating/TemplateAnnotator.cs`, `Templating/TemplateCompilerRewriter.cs` and the
generated meta-syntax rewriter — outside this subsystem.

### 4.4 A NEW collection-expression element (for example `with(...)`)

No touch point in this subsystem. `AdviceImpl` never parses or rewrites a collection expression; it only serialises
`IExpression` values through `IExpression.ToExpressionSyntax` (`AdviceSyntaxGenerator.cs:140-147`). The work belongs
to `Templating/` and `SyntaxSerialization/`.

### 4.5 A NEW optional field on an existing statement (labelled `break` / `continue`)

No touch point in this subsystem either, for the same reason. The one indirect consequence: templates that use a
labelled `break` must round-trip through template compilation, which is `Templating/`. The generated
`MetaSyntaxRewriter` from `eng/src/GenerateMetaSyntaxRewriter` is regenerated from `Syntax-5.10.0.xml`, which already
declares the optional `Name` field on `BreakStatementSyntax` and `ContinueStatementSyntax`.

---

## 5. Places that would silently do the wrong thing

Ordered by how likely a C# 15 construct is to reach them.

1. **`AdviceFactory.IntroduceFinalizer` (lines 730-751) has no `ValidateNotExtensionBlock`.**
   `AdviceKind.IntroduceFinalizer` maps to `_introduceRule` (`EligibilityRuleFactory.cs:250-251`), which admits
   `TypeKind.Extension` (line 121). Introducing a finalizer into an extension block therefore passes validation and
   reaches `IntroduceMethodTransformation`'s `MethodKind.Finalizer` arm (line 49), which builds a
   `DestructorDeclaration` named after `finalMethod.DeclaringType.GetPrimaryDeclarationSyntax()` — the extension
   block's own declaration. The result is invalid C# emitted without a Metalama diagnostic. There is no
   `ErrorFinalizerIntoExtensionBlock` test.

2. **`AdviceFactory.IntroduceEvent` — the add/remove overload (line 1513) has no `ValidateNotExtensionBlock`,
   while its sibling at line 1478 has one at line 1490.** `ErrorEventIntoExtensionBlock.cs` only exercises the
   guarded overload (`extensionBlock.IntroduceEvent( nameof(MyEvent) )`). The unguarded overload emits an event
   declaration into an `extension` block, which C# rejects — again with no Metalama diagnostic.

3. **`IntroduceIndexerTransformation` does not override `GetImplicitDeclarations()`.**
   `IntroduceMethodTransformation` (line 228) and `IntroducePropertyTransformation` (line 226) do;
   `IntroduceIndexerTransformation` (139 lines, only `GetInjectedMembers` at line 28) inherits
   `BaseTransformation.GetImplicitDeclarations()` (line 63), which returns `Enumerable.Empty<…>()`.
   The moment `AdviceFactory.cs:1406` is deleted for C# 15, an introduced extension indexer will be injected into
   the extension block but the static implementation methods (`get_Item` / `set_Item`) will never be added to the
   code model, so invokers and the linker will not see them. Nothing throws; the code model is simply incomplete.

4. **`ContractExtensionBlockTransformation.GetInsertedStatements` enumerates only `Methods`, `Properties` and
   `Indexers` (lines 75, 80, 93).** A future extension member kind (events, constructors, fields) receives no
   receiver contract and no diagnostic. The `.Where( … !IsStatic )` filters are deliberate and documented, but they
   also mean that if C# ever gives a static extension member a receiver, the contract is silently skipped.

5. **`ImplementInterfaceAdvice` lines 184-211 skip interface indexers by design**
   (`// Indexers are ignored, because there are no indexer templates.`, line 196). The aspect gets no diagnostic; the
   interface is declared as implemented while the indexer is not implemented, and the failure surfaces as a raw
   Roslyn CS0535 on generated code. The explicit-specification path does throw
   (`NotImplementedException` at line 780), so the silent path is only the declarative one.

6. **`AdviceSyntaxGenerator.GetAttributeLists` (lines 40-63) has no default arm.** For a declaration kind it does not
   know, it returns the declaration's own attributes and drops any that belong to an implicit sub-declaration.
   Line 61 already carries `// TODO: field-level attributes`.

7. **`IntroduceMemberAdvice` silently downgrades `IsVirtual` twice**, at lines 136-141 and 237-243:
   `// Silently ignore IsVirtual when the target type is sealed or a struct`. The condition is
   `targetDeclaration.IsSealed || targetDeclaration.TypeKind == TypeKind.Struct` — a new value-like type kind is not
   covered, so a virtual member would be emitted into a type that cannot have one.

8. **`ValidateNotExtensionBlockReceiver` (line 538) identifies the extension receiver structurally**, as
   `IParameter { DeclaringMember: null }`. Any future parameter whose `DeclaringMember` is null for another reason
   would be reported as an extension receiver, with a wrong message. Conversely, if the code model ever gives the
   receiver a non-null `DeclaringMember`, the guard silently stops firing.

9. **`OverrideHelper.ComputeBackingFieldName` (lines 143-171) checks collisions against `AllFields`,
   `AllProperties`, `AllEvents` and `AllMethods` only.** It does not consult `AllTypes` or `AllIndexers`. A nested
   type whose name matches the computed `_camelCase` hint produces a genuine C# name collision rather than a
   Metalama diagnostic. The loop is bounded (`for ( var i = 1; i < int.MaxValue; i++ )`) and throws only on
   exhaustion.

10. **`ContractBaseTransformation.ToDisplayString` (line 119) falls through to
    `_ => $"unexpected declaration '{target}'"`** instead of asserting. This is display-only, but it means an
    unexpected contract target reaches the introspection and linker-log output as text rather than failing a test.

11. **`_introduceRule` (`EligibilityRuleFactory.cs:117-125`) admits exactly
    `TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension`.** `TypeKind.RecordClass` and
    `TypeKind.RecordStruct` exist in the enum (`Code/TypeKind.cs:30,68`) but are never produced by
    `SourceNamedTypeImpl.TypeKind` (records map to `TypeKind.Class`). This is a latent trap rather than a live bug:
    the day the code model starts reporting a distinct record kind, every `Introduce*` advice on a record becomes
    ineligible with a message about classes, structs and interfaces, and no code in this subsystem changes.

12. **`Roslyn.5.0.0.props` defines no constant and `updating-roslyn.md` step 12 discourages adding one.** A C# 15
    syntax factory called unconditionally from `AdviceImpl` compiles against the latest variant and throws
    `MissingMethodException` at run time in a Roslyn 5.0 host, which surfaces as an aspect-level `LAMA0041` rather
    than as a supported-version diagnostic.

---

## 6. Quick index of the highest-value line numbers

```
AdviceFactory.cs:527   ValidateNotExtensionBlock          (the gate)
AdviceFactory.cs:536   ValidateNotExtensionBlockReceiver
AdviceFactory.cs:1406  ValidateNotExtensionBlock(targetType, "an indexer")     <- delete for C# 15
AdviceFactory.cs:730   IntroduceFinalizer                 (missing gate)
AdviceFactory.cs:1513  IntroduceEvent add/remove overload (missing gate)
AdviceFactory.cs:2068  TypeKind.Class literal (IntroduceClass)
AdviceFactory.cs:2090  TypeKind.Interface literal (IntroduceInterface)
ContractExtensionBlockTransformation.cs:93   foreach extensionBlock.Indexers   <- already speculative
IntroduceMemberAdvice.cs:197                 TypeKind.Extension carve-out
IntroduceNamedTypeTransformation.cs:62-92    TypeKind -> declaration syntax
IntroduceMethodTransformation.cs:228         GetImplicitDeclarations (methods)
IntroducePropertyTransformation.cs:226       GetImplicitDeclarations (properties)
IntroduceIndexerTransformation.cs            (no GetImplicitDeclarations)      <- add for C# 15
ExtensionImplementationHelper.cs:177         "set_"/"get_" + propertyName      <- no indexer form
ImplementInterfaceAdvice.cs:196              "Indexers are ignored"
ImplementInterfaceAdvice.cs:780              NotImplementedException, interface indexers
TemplateMember.cs:61-70                      IntroducesBackingField / IsBackingFieldAssigned
TemplateBindingHelper.cs:119-120             OperatorCategory.UnaryAssignment / BinaryAssignment
OverrideHelper.cs:143                        ComputeBackingFieldName
AdviceKind.cs:50                             IntroduceExtensionBlock (last enum value)
EligibilityRuleFactory.cs:121                _introduceRule TypeKind allow-list
AdviceDiagnosticDescriptors.cs:302-319       LAMA0540 / LAMA0541
```
