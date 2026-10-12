# Attribute-bound aspects

This document proposes a declarative way to bind an aspect class to a custom attribute that the aspect does not
own, so that every declaration carrying the attribute receives an instance of the aspect without a fabric. It is
a design proposal for a separate user story from [aspects projects](aspects-projects.md), which motivates it but
does not depend on it. Nothing described here is implemented. The file and line references describe
`develop/2027.0` in October 2026.

## 1. The problem

An aspect is applied to a declaration in one of two ways today: the aspect class is itself a custom attribute
and the user writes it on the declaration, or a fabric selects declarations and calls `AddAspect`. The first way
requires the project that applies the attribute to reference the aspect class. The second way requires a fabric
for a binding that is often one line of intent: "every declaration marked `[Log]` gets a `LogAspect`".

Two situations need that binding and cannot use the first way:

- An aspects project ([aspects projects](aspects-projects.md)) whose aspects react to attributes declared in a
  contracts project. The business code applies the contract attribute and never sees the aspect class.
- A Metalama project that reacts to attributes of a third-party framework, for example a logging aspect on every
  method marked `[HttpGet]`, or a validation aspect on every type marked with an object-relational mapper's
  `[Entity]`. Nobody can turn those attributes into aspects.

Both situations are served by a fabric today, with two costs. The fabric is boilerplate, and the diagnostics of
an ineligible target point at the fabric class rather than at the attribute that caused the aspect to be added
(`Metalama.Framework.Engine/Queries/Aspects/AspectQuerySource.cs:63-86`, LAMA0039, reported at the predecessor's
location, which `FabricDriver.GetDiagnosticLocation` resolves to the fabric type). The user then has to find the
declaration from the message.

## 2. The proposal

A binding attribute on the aspect class names the attribute type that applies it:

```csharp
using Contoso.Contracts;
using Metalama.Framework.Aspects;

namespace Contoso.Aspects;

[AppliedBy( typeof(LogAttribute) )]
public sealed class LogAspect : OverrideMethodAspect
{
    private readonly string? _category;

    public LogAspect( IAttribute attribute )
    {
        this._category = attribute.GetArgumentValue<string>( "category" );
    }

    public override dynamic? OverrideMethod() { /* ... */ }
}
```

The business code applies the contract attribute only:

```csharp
[Log( "Orders" )]
public void PlaceOrder( Order order ) { /* ... */ }
```

The engine creates one `LogAspect` instance per `[Log]` attribute, with the attribute as the predecessor of the
instance, exactly as it does today for an aspect applied as an attribute.

### 2.1. The binding attribute

```csharp
namespace Metalama.Framework.Aspects;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
[CompileTime]
public sealed class AppliedByAttribute : Attribute
{
    public AppliedByAttribute( Type attributeType );

    public Type AttributeType { get; }

    public bool IncludeDerivedTypes { get; init; } = true;

    public IneligibilityHandling OnIneligible { get; init; } = IneligibilityHandling.Error;
}

public enum IneligibilityHandling
{
    Error,
    Warning,
    Ignore
}
```

The attribute is defined in `Metalama.Framework`, not in `Metalama.Framework.Redist`: it is applied to aspect
classes, which always reference the framework, and the contracts assembly never needs it.

The binding sits on the aspect and names the attribute, never the reverse. The contracts project must not know
the aspects, and in the aspects topology it cannot reference them without a cycle. A third-party attribute
cannot be annotated at all. The reverse form, naming the aspect by a string on the contract attribute, was
considered and rejected: it loses the compile-time check of the name, does not cover third-party attributes,
and moves a decision about aspects into the project that was meant to know nothing about them.

`AllowMultiple` lets one aspect serve several attribute types. Two aspect classes bound to the same attribute
type both apply, ordered by the usual aspect ordering. Neither case needs a rule of its own.

`IncludeDerivedTypes` defaults to `true`, as it does on `SelectDeclarationsWithAttribute`
(`Metalama.Framework/Fabrics/QueryExtensions.cs:49-69`) and on `IAttributeCollection.OfAttributeType`.

`OnIneligible` is described in section 4.

### 2.2. The constructor of the aspect

The engine instantiates the aspect through one of three constructors, chosen once per binding when the aspect
class is created:

1. A constructor with a single parameter of the bound attribute type, or a base type of it. The engine constructs
   the attribute with `AttributeDeserializer` (`Metalama.Framework.Engine/CompileTime/AttributeDeserializer.cs:63-91`)
   and passes the instance. This requires the attribute type to be available at compile time, which is the typed
   path of the aspects projects document: the type is marked `[RunTimeOrCompileTime]` and its assembly is loadable in the
   compile-time domain.
2. A constructor with a single `IAttribute` parameter. The engine passes the code model representation of the
   attribute. This works with any attribute type, including a type from a thick framework assembly that is never
   loaded, because `IAttribute` exposes the constructor arguments and named arguments as data.
3. The default constructor. The aspect ignores the attribute and reacts to its presence only.

The engine prefers them in that order and reports an error at the binding when none applies. It does not match
constructor parameters to attribute arguments by name: a one-line constructor body replaces that rule, and the
rule would fail silently on a renamed argument.

When the first constructor is chosen and the attribute type cannot be loaded, which happens with a thick
contracts assembly, the engine reports LAMA0401 at the binding, with the hint to use the `IAttribute`
constructor.

An inheritable bound aspect must not store the attribute instance or the `IAttribute` in a field: its instances
are serialized into the inheritable aspects manifest, and a contract attribute type cannot implement
`ICompileTimeSerializable`, which is a compile-time interface. The aspect copies the values it needs into its
own fields. The [Patterns study](aspects-projects-patterns-study.md), gap 5, records the alternative of moving
the marker interface to the run-time package.

### 2.3. Options providers

The [Patterns study](aspects-projects-patterns-study.md), gap 2, shows that configuration attributes need the
same binding. A configuration attribute today implements `IHierarchicalOptionsProvider` and returns
`IHierarchicalOptions` instances, which a run-time package cannot do, because both interfaces are in
`Metalama.Framework` and the second is compile-time. `[AppliedBy]` is therefore accepted on a class that
implements `IHierarchicalOptionsProvider` as well as on an aspect class:

```csharp
[AppliedBy( typeof(CachingConfigurationAttribute) )]
internal sealed class CachingConfigurationProvider : IHierarchicalOptionsProvider
{
    public CachingConfigurationProvider( CachingConfigurationAttribute attribute ) { /* ... */ }

    public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context ) { /* ... */ }
}
```

`CompilationHierarchicalOptionsSource` (`Metalama.Framework.Engine/HierarchicalOptions/CompilationHierarchicalOptionsSource.cs:48-92`)
instantiates the provider once per bound attribute through the constructor rules of section 2.2 and calls
`GetOptions` with the attribute's containing declaration as the target, as it does today for a provider
attribute. `OnIneligible` does not apply to providers; option eligibility is checked by the options manager
as today (LAMA0065). A bound aspect class that also implements `IHierarchicalOptionsProvider` needs nothing
more: `AspectDriver` applies its options before `BuildAspect`, as for any aspect.

## 3. Mechanism

### 3.1. Reading the binding

The compile-time code rewriter removes from the compile-time copy of a class every attribute whose class is a
run-time-only type and keeps the others
(`Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:148-186`).
`typeof` expressions are rewritten to `TypeOfResolver.Resolve` calls only inside expressions
(`ProduceCompileTimeCodeRewriter.cs:1649-1665`, `TypeOfRewriter.cs:30-69`); an attribute argument cannot be
rewritten into a method call. A compile-time `[AppliedBy(typeof(LogAttribute))]` would therefore be kept in the
compile-time copy with its `typeof` intact, and the compile-time compilation would fail to bind `LogAttribute`
whenever the contracts assembly is not referenced by the compile-time compilation, which is the case for every
thick contracts assembly.

The binding is therefore not read from the compile-time compilation. Three steps replace that:

- The rewriter removes `[AppliedBy]` from the compile-time copy, as it already removes run-time-only attributes.
  The attribute is metadata for the manifest and has no use inside the compile-time assembly.
- The manifest builder (`CompileTimeCompilationBuilder.cs:1215-1311`) reads `[AppliedBy]` from the run-time
  compilation's symbol of each aspect type, where `typeof(LogAttribute)` resolves because the contracts assembly
  is an ordinary reference of the project, and records for each binding the metadata name of the attribute type,
  its assembly name, `IncludeDerivedTypes` and `OnIneligible`.
- `CompileTimeProjectManifest` (`Metalama.Framework.Engine/CompileTime/Manifest/CompileTimeProjectManifest.cs`)
  gains an `AttributeBindings` list next to `AspectTypes`. The field is additive with an empty default, so the
  manifest version stays at 1 and existing aspect libraries do not have to be recompiled.

An untransformed compile-time assembly (`CompileTimeProject.TryCreateUntransformed`,
`Metalama.Framework.Engine/CompileTime/CompileTimeProject.cs:337-459`), which is loaded as is and reflected over,
has no rewriter and keeps the attribute. Reading `[AppliedBy]` through reflection would resolve the `Type`
argument and fail for a thick contracts assembly. The untransformed path reads the binding through
`System.Reflection.Metadata` instead, which gives the type name as a string without loading it. The
`MetadataReader` utilities in `Metalama.Framework.Engine/Utilities/MetadataReader.cs` already read custom
attributes that way.

`AspectClass` exposes the bindings from the manifest. The validator must allow a `typeof` of a run-time-only
type in the arguments of `[AppliedBy]`, which it already allows in expressions; this is to be verified when the
validator is touched.

### 3.2. Creating the aspect instances

`CompilationAspectSource` (`Metalama.Framework.Engine/Aspects/CompilationAspectSource.cs:73-133`) is the aspect
source that turns custom attributes into aspect instances. Today it looks up the attributes of the aspect type
itself, deserializes each attribute as the aspect, and creates the instance with the attribute as predecessor
through `AspectClass.CreateAspectInstanceFromAttribute` (`AspectClass.cs:278-282`). The proposal extends the
same source: for each binding of the aspect class, it looks up the attributes of the bound type with
`GetAllAttributesOfType(type, includeDerivedTypes)` (`Metalama.Framework.Engine/CodeModel/CompilationModel.cs:465-497`),
resolves the bound type by its metadata name in the business compilation, instantiates the aspect through the
constructor chosen in section 2.2, under `UserCodeInvoker` so that an exception in the constructor is reported as
a user code error at the attribute, and creates the instance with the attribute as predecessor.

Nothing else in the pipeline distinguishes an attribute-bound instance from an attribute instance. Eligibility,
ordering, inheritance, diagnostics, the introspection model and the design-time features all see an aspect
instance whose predecessor is an attribute.

### 3.3. Exclusions

`[ExcludeAspect(typeof(LogAspect))]` (`CompilationAspectSource.cs:44-69`) continues to exclude the aspect on a
declaration and its children. It names the aspect, which the business code cannot reference in the aspects
topology, so the exclusion is available only where the aspect class is visible. A future addition could accept
the bound attribute type as well; it is not part of this proposal.

## 4. Eligibility

### 4.1. How ineligibility is reported today

An aspect applied as an attribute is guarded three times. The C# compiler enforces the `[AttributeUsage]` of the
aspect base class (`OverrideMethodAspect` allows `AttributeTargets.Method`, `TypeAspect` allows the type kinds),
so a misplaced aspect attribute is CS0592 at the attribute before Metalama runs. `CompilationAspectSource` then
computes the eligibility of the instance and reports LAMA0037, "The aspect 'X' cannot be applied to the method
'Y' because …", at the attribute (`CompilationAspectSource.cs:108-126`). Finally `AspectDriver` reports LAMA0003
at the target declaration when the aspect does not implement `IAspect<T>` for the target kind
(`Metalama.Framework.Engine/Aspects/AspectDriver.cs:157-171`), with a comment asking whether the attribute
would be the better location; in practice the eligibility rules, which include the declaration type, catch this
first.

An aspect added by a fabric is guarded once, by `AspectQuerySource`: `AddAspectIfEligible` skips the declaration
silently, and `AddAspect` reports LAMA0039 at the fabric.

### 4.2. What changes

The first guard is lost. The contract attribute's own `[AttributeUsage]` governs where it can be written, and the
contracts author decides it without knowing the aspect; a third-party attribute's usage is fixed. The second
guard replaces it, with three adjustments.

The diagnostic is reported at the bound attribute, which is the predecessor, with no extra work. Its wording
changes for the bound case, because the user wrote `[Log]` and has never seen `LogAspect`: "The attribute [Log]
cannot be applied to the method 'Y' because the aspect 'LogAspect' requires …". This is a new descriptor, since
LAMA0037 is also used for the direct case and its wording is right there.

The severity follows `OnIneligible`. `Error` is the default and the right choice for an attribute the user owns:
a `[Log]` on an ineligible declaration is a mistake. `Warning` and `Ignore` serve the third-party case, where the
aspect author binds to an attribute whose placement nobody controls and expects to meet declarations where the
aspect does not apply. This is the declarative counterpart of the choice between `AddAspect` and
`AddAspectIfEligible`. With `Warning` or `Ignore`, no instance is created for the ineligible declaration; with
`Error`, the instance is created as today, so that later errors are not hidden.

The inheritance semantics do not change. An instance whose eligibility is `Inheritance` only, on an abstract or
virtual member, is not an error today and does not become one.

The comment at `AspectDriver.cs:159` is resolved on the way: when the predecessor of the instance is an
attribute, LAMA0003 is reported at the attribute.

### 4.3. A static check when the aspects project compiles

The engine never compares the eligibility of an aspect with the `AttributeUsage` of an attribute; nothing in
`AspectClass` or `EligibilityHelper` reads `AttributeTargets`. For a bound attribute, a mismatch is a design
error of the binding, and it should be reported once, where the binding is written, rather than at every
misplaced attribute in every business project.

When `AspectClass` is created for the compile-time project of the project that declares the aspect, which
happens when the aspects project itself compiles, the engine reads the `[AttributeUsage]` of the bound attribute
type from the run-time compilation, maps each `AttributeTargets` flag to the declaration interface that
`IsEligibleFast` uses (`AspectClass.cs:381-410`), and checks that the aspect implements `IAspect<T>` for each.
A flag with no matching `IAspect<T>` is reported at the `[AppliedBy]` attribute as a warning when `OnIneligible`
is `Error`, and not at all otherwise, since the aspect author has stated that ineligible placements are
expected. The check covers the declaration kind only; the finer eligibility rules are predicates over
declarations and cannot be evaluated without one.

## 5. Relation to the fabric helper

The aspects projects document proposes `AddAspectForAttribute`, a fabric helper that selects by attribute, constructs the
aspect from the attribute and reports ineligibility at the attribute. The two are complementary. `[AppliedBy]`
is the recommended form and covers the common case with no code. The helper remains for computed bindings: a
predicate on the attribute, a choice of aspect that depends on the target, or a binding that the aspect author
does not own. Both report at the attribute, through the same predecessor mechanism.

## 6. Changes by component

| Component | Change |
|---|---|
| `Metalama.Framework` | `AppliedByAttribute` and `IneligibilityHandling` in `Metalama.Framework.Aspects`. |
| `ProduceCompileTimeCodeRewriter` | Remove `[AppliedBy]` from the compile-time copy. |
| `CompileTimeCompilationBuilder`, `CompileTimeProjectManifest` | Read the bindings from the run-time symbol of each aspect type; additive `AttributeBindings` field. |
| `CompileTimeProject.TryCreateUntransformed` | Read the bindings through `System.Reflection.Metadata`. |
| `AspectClass` | Expose the bindings; choose the constructor per binding; the static `AttributeUsage` check of section 4.3. |
| `CompilationAspectSource` | Create instances for bound attributes; the new ineligibility descriptor; honour `OnIneligible`. |
| `AspectDriver` | Report LAMA0003 at the attribute when the predecessor is an attribute. |
| `TemplatingCodeValidator` | Allow `typeof` of a run-time-only type in the arguments of `[AppliedBy]`, if it does not already. |
| Tests | Aspect tests for the three constructors, derived attribute types, two bindings on one aspect, two aspects on one attribute, each `OnIneligible` value, the warning of section 4.3, a thick attribute type that is never loaded, and the untransformed path. |
| Documentation | A conceptual article under "Applying aspects"; a mention in the aspects projects article. |

## 7. Open questions

1. Whether `[ExcludeAspect]` should accept the bound attribute type (section 3.3).
2. Whether the static check of section 4.3 should be an error rather than a warning. A warning is proposed
   because the check is approximate, and an aspect that is eligible for a kind only under conditions is not
   wrong.
3. The name. `AppliedBy` reads in the direction of the relationship and reuses the vocabulary of the "applying
   aspects" documentation. `ForAttribute` was considered: it is shorter, but its class name is awkward and
   "for" does not state the direction.
