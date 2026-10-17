# A general naming-conventions package

This document sets a direction for a future design. It proposes a public package, `Metalama.Extensions.NamingConventions`,
that gives aspect authors one way to derive names from other names, to find companion members by name, and to check
that names follow a pattern. It is not a design: it records the use cases, the shape of the API and the risks, so
that a design can start from them. Nothing described here is implemented. The file and line references describe
`develop/2027.0` in October 2026, with paths relative to the repository root unless stated otherwise.

## 1. Motivation

Several Metalama features process names, and each one implements the logic in its own way.

The naming-convention infrastructure of `Metalama.Patterns.Wpf`
(`Metalama.Patterns/src/Metalama.Patterns.Wpf/Implementation/NamingConvention/`) is the most complete
implementation. It is internal, and its only consumers are the `[Command]` and `[DependencyProperty]` aspects. The
[split of the commands into their own package](commands-package.md) needs this code in two packages. The
constraints of that split are that no package grants `InternalsVisibleTo` to another package, that no source code is
shared between packages, and that the naming-convention code does not belong in a package named
`Metalama.Patterns.Commands`. A public package is the only option that satisfies all three constraints without
duplicating the code. The current code is not suitable as a public API (section 3), so the package must be
designed rather than extracted.

## 2. Use cases

### 2.1. Existing code

| Feature | What it does with names | Where |
|---|---|---|
| `[Command]` | Removes the `_` and `m_` prefixes, the `Execute` prefix and the `Async` and `Command` suffixes from the method name to obtain a command name. Derives `{Name}Command` for the generated property. Looks for `CanExecute{Name}`, `Can{Name}` and `Is{Name}Enabled` as a method or as a property. | `Metalama.Patterns/src/Metalama.Patterns.Wpf/Implementation/CommandNamingConvention/DefaultCommandNamingConvention.cs:38-49,74-77` |
| `[DependencyProperty]` | Derives `{Name}Property` for the registration field. Looks for `On{Name}Changed` and `Validate{Name}`. | `Metalama.Patterns/src/Metalama.Patterns.Wpf/Implementation/DependencyPropertyNamingConvention/DefaultDependencyPropertyNamingConvention.cs:31-35` |
| `[Observable]` | Looks for an overridable method in the base class among three fixed names: `OnPropertyChanged`, `NotifyOfPropertyChange` and `RaisePropertyChanged`. The list cannot be configured. | `Metalama.Patterns/src/Metalama.Patterns.Observability/Implementation/ClassicStrategy/ClassicObservabilityStrategyImpl.cs:23,879` |
| Dependency injection | Derives a field name from a type name by adding `_` and lowercasing the first letter. Derives a constructor parameter name from a field name by removing the leading underscores. A comment proposes to read the naming rules from `.editorconfig`. | `Metalama.Extensions/src/Metalama.Extensions.DependencyInjection/DependencyInjectionExtensions.cs:166-168`, `Metalama.Extensions/src/Metalama.Extensions.DependencyInjection/Implementation/DefaultParameterPullStrategy.cs:150-153` |
| Architecture (Metalama.Premium) | Checks that the names of declarations, or of the types derived from a base type, match a wildcard pattern or a regular expression. | `MustRespectNamingConvention`, `DerivedTypesMustRespectNamingConvention` and `DerivedTypesMustRespectRegexNamingConvention` in `Metalama.Extensions.Architecture/ArchitectureExtensions.cs` |

### 2.2. Aspects that users and future pattern libraries write

- Hook methods: `On{Name}Changing`, `On{Name}Changed`, `OnBefore{Name}`, `OnAfter{Name}`.
- The designer conventions of Windows Forms controls: `ShouldSerialize{Name}`, `Reset{Name}` and a `{Name}Changed`
  event.
- Validation methods for `INotifyDataErrorInfo`: `Validate{Name}`.
- Generated code in the style of builders and clones: `With{Name}`, `Is{Name}Set`, backing fields `_{name}`.
- Property aspects for other user interface frameworks, if they are ever written: `Coerce{Name}` and the registration
  field names of Avalonia and .NET MAUI.

### 2.3. Operations

The use cases need three operations:

1. Derive a name from another name, for example a command property name from a method name.
2. Find the members of a type whose names are derived from a target name, check their signatures, and report a
   precise diagnostic when a member is missing, ambiguous, or has the wrong signature.
3. Check that a name follows a pattern.

## 3. Limitations of the current code as a public API

- The extension point is an interface, `INamingConvention<TDeclaration, TMatch>`, whose implementation returns a
  `NamingConventionMatch` record that each aspect subclasses. An aspect author must write a match type, a matcher and a
  subclass of `DiagnosticReporter` to look up one companion member.
- The categories of companion members are untyped strings, and they are inserted into the diagnostic messages.
- The diagnostics (LAMA5203 to LAMA5207) and their arguments assume a generated member and named companion members.
  Their identifiers belong to the range of `Metalama.Patterns.Wpf`.
- The conventions must implement `ICompileTimeSerializable`, because they are stored in hierarchical options. A public
  API would impose this constraint on every convention that a user writes.
- The name derivation of the default command convention is written as code (`TrimStart` and `TrimEnd` calls), while
  the configurable conventions use regular expressions and a `{CommandName}` token. The two cannot be combined.
- The code has no tests of its own. Its README states that it is tested through the aspects.

## 4. Direction for the API

### 4.1. A name pattern that parses and formats

A `NamePattern` is a template that can be used in both directions: to parse a name into named parts, and to format
named parts into a name.

```csharp
var target = NamePattern.Parse( "[Execute]{Stem}[Async]" ); // "ExecuteSaveAsync" is parsed as Stem = "Save".
var command = NamePattern.Parse( "{Stem}Command" );         // Stem = "Save" is formatted as "SaveCommand".
var field = NamePattern.Parse( "_{stem}" );                 // The lowercase token formats "Save" as "_save".
```

- `{Stem}` and `{stem}` select the casing of the part when the pattern formats a name.
- Square brackets mark an optional literal.
- `*` is an unnamed part. The wildcard patterns of the Architecture package are therefore a subset of the language.
- A regular expression with named groups is accepted for the cases that the template syntax cannot express.

The existing `{CommandName}` token of `CommandNamingConvention` shows that users already accept this kind of syntax.

### 4.2. A declarative description of a convention

The aspect author describes the convention as data. The author does not subclass a match type, a matcher or a
reporter.

```csharp
var convention = new NamingConvention( "default" )
{
    Target = NamePattern.Parse( "[Execute]{Stem}[Async]" ),
    Companions =
    {
        Companion.Optional( "can-execute", NamePattern.Parse( "CanExecute{Stem}" ), NamePattern.Parse( "Can{Stem}" ) )
            .MethodOrProperty( returns: SpecialType.Boolean ),
    },
    Generated = { Generated.Property( "command", NamePattern.Parse( "{Stem}Command" ) ) },
};
```

The roles of the companion members and of the generated members are identified by keys. The design must decide
whether these keys are strings or typed identifiers, because strings are the weakness of the current code.

### 4.3. A resolver with typed results and standard diagnostics

```csharp
if ( NamingConventionResolver.TryResolve( builder, method, conventions, out var resolution ) )
{
    var canExecute = resolution.GetCompanion( "can-execute" ); // An IMethod, an IProperty or null.
    var commandName = resolution.GetGeneratedName( "command" );
}
```

The resolver tries the conventions in priority order and stops at the first one that matches. It checks that each
generated name does not conflict with an existing member. It reports the diagnostics itself, because the description
of the convention contains the information that the messages need: the role, the patterns that were tried and the
expected signature. These diagnostics are the main value of the package, because they are the part of the work that
is the most difficult to write correctly in each aspect.

### 4.4. Configuration

- A serializable base type for the hierarchical options of an aspect, with the registration and priority mechanism
  that `NamingConventionRegistration` provides today.
- A standard way for an attribute property, such as `CanExecuteMethod` on `[Command]`, to give an explicit name that
  takes precedence over every convention.
- Optionally, reading the naming rules of `.editorconfig` (`dotnet_naming_rule` and `dotnet_naming_style`) to obtain
  the prefix and the casing of fields and parameters, as the comment in the dependency injection code proposes.

### 4.5. Code fixes

When a required companion member is missing, ambiguous, or has the wrong signature, the resolver attaches code fixes
to the diagnostic: rename an existing member to the expected name, or add a member with the expected signature. The
Metalama code fix API supports this, so every aspect that uses the package receives the code fixes without writing
them.

### 4.6. Checking names

The Architecture package uses `NamePattern` for its naming rules. A team then uses one syntax both to generate names
and to check them.

## 5. Placement

`Metalama.Extensions` is built before `Metalama.Patterns` (`eng/src/Program.cs:127-128`), so the pattern libraries can
reference the new package. `Metalama.Premium`, which contains the Architecture package, already references packages of
`Metalama.Extensions`.

The package needs its own range of diagnostic identifiers. The diagnostics LAMA5203 to LAMA5207 of the pattern
libraries are then retired or kept as aliases. The design must decide which, because the identifiers appear in the
suppressions of user code.

## 6. Risks and open questions

1. Ambiguous parsing. With optional literals, one name can be parsed in more than one way. For example,
   `[Execute]{Stem}[Async]` can parse `ExecuteAsync` with an empty stem or with the stem `Execute`. The rules must be
   precise (for example, a part cannot be empty, and the parse that uses the most literals wins), and they need
   thorough tests.
2. Serialization and memory. Conventions are stored in hierarchical options and kept by the design-time process.
   They must be immutable and compile-time serializable, and they must not reference the code model.
   `System.Text.RegularExpressions.Regex` is already registered as a durable type for this reason
   (`Metalama.Framework/src/Metalama.Framework.Analyzers/WellKnownDurableTypes.cs:91`).
3. Generality of the API. Before the API is frozen, at least two consumers with different needs must be ported to it:
   the commands, which use companion members, and dependency injection, which only derives names. The Architecture
   naming rules are a good third consumer.
4. Cost. The package is a public API with documentation, tests, diagnostics and a compatibility commitment. The design
   must confirm that the use cases of section 2 justify this cost.
5. Compatibility of the existing options. `CommandNamingConvention` and `DependencyPropertyNamingConvention` are public
   records with regular-expression patterns. The design must decide whether they are kept as a facade over the new
   API or replaced.

## 7. Relation to the commands split

If this package exists, `Metalama.Patterns.Commands` and `Metalama.Patterns.Wpf` both reference it, and the question of
where the naming-convention code lives disappears. If the commands split happens first, each package needs its own
copy of the naming-convention code until both are ported to this package.
