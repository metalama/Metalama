// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// Decides whether a type is immutable, for one compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every mutable field of this analyzer lives here, and an instance of this class is created inside
    /// <c>RegisterCompilationStartAction</c> and captured only by the actions registered on that context.</b> Roslyn
    /// keeps one instance of a <see cref="DiagnosticAnalyzer"/> alive for the lifetime of the process, so a cache of
    /// symbols held in a field of the analyzer would retain the compilation those symbols came from. That is the very
    /// defect the sibling <c>Durable</c> contract of this same assembly exists to report.
    /// </para>
    /// <para>
    /// No cycle can arise while evaluating a type, because the evaluation never descends into the members of a type:
    /// a type is immutable when it is marked or well known, and is otherwise not, so the recursion follows only
    /// nullable arguments, tuple elements and type arguments. Those form a finite tree. The budget below therefore
    /// guards against pathological nesting rather than against non-termination.
    /// </para>
    /// <para>
    /// Deep immutability is nonetheless achieved, by closure over the marked types rather than by structural
    /// inference: a marked type is itself verified, where it is declared, to have only read-only fields of immutable
    /// types. Descending into an unmarked type instead would introduce real cycles, and would read the wrong field
    /// list for a type of a referenced assembly, whose private fields a compilation created with
    /// <c>MetadataImportOptions.Public</c> does not expose at all.
    /// </para>
    /// </remarks>
    internal sealed class ImmutabilityContext
    {
        private const int _maxDepth = 20;

        /// <summary>
        /// The full metadata name of the marker, matched by name because this project deliberately references only
        /// Roslyn.
        /// </summary>
        public const string ImmutableObjectAttributeMetadataName = "System.ComponentModel.ImmutableObjectAttribute";

        /// <summary>
        /// The full metadata name of the type whose presence gates the whole analyzer.
        /// </summary>
        /// <remarks>
        /// The sibling contract gates on its own attribute, which makes it free for a project that does not reference
        /// Metalama. That does not work here: <see cref="ImmutableObjectAttributeMetadataName"/> lives in
        /// <c>System.ComponentModel.Primitives</c> and resolves in every compilation, so gating on the marker would
        /// gate on nothing. Gating on the subject of the contract costs the same one failed lookup.
        /// </remarks>
        public const string AspectInterfaceMetadataName = "Metalama.Framework.Aspects.IAspect";

        private readonly ConcurrentDictionary<ITypeSymbol, ImmutabilityVerdict> _verdicts;
        private readonly ConcurrentDictionary<INamedTypeSymbol, bool> _isSubjectToContract;
        private readonly ConcurrentDictionary<INamedTypeSymbol, ulong> _storedTypeParameters;
        private readonly ImmutableHashSet<string> _additionalImmutableTypes;
        private readonly ImmutableHashSet<string> _additionalMutableTypes;
        private readonly ImmutableHashSet<string> _additionalContractTypes;

        private ImmutabilityContext(
            ImmutableHashSet<string> additionalImmutableTypes,
            ImmutableHashSet<string> additionalMutableTypes,
            ImmutableHashSet<string> additionalContractTypes )
        {
            this._verdicts = new ConcurrentDictionary<ITypeSymbol, ImmutabilityVerdict>( SymbolEqualityComparer.Default );
            this._isSubjectToContract = new ConcurrentDictionary<INamedTypeSymbol, bool>( SymbolEqualityComparer.Default );
            this._storedTypeParameters = new ConcurrentDictionary<INamedTypeSymbol, ulong>( SymbolEqualityComparer.Default );
            this._additionalImmutableTypes = additionalImmutableTypes;
            this._additionalMutableTypes = additionalMutableTypes;
            this._additionalContractTypes = additionalContractTypes;
        }

        /// <summary>
        /// Creates the context of a compilation, or returns <c>null</c> when the compilation does not know
        /// <c>IAspect</c>, in which case no action is registered and the analyzer costs one failed symbol lookup.
        /// </summary>
        public static ImmutabilityContext? TryCreate( Compilation compilation, AnalyzerOptions options )
        {
            if ( compilation.GetTypeByMetadataName( AspectInterfaceMetadataName ) == null )
            {
                return null;
            }

            var globalOptions = options.AnalyzerConfigOptionsProvider.GlobalOptions;

            return new ImmutabilityContext(
                SymbolFacts.ReadTypeNameList( globalOptions, "build_property.MetalamaImmutableTypes" ),
                SymbolFacts.ReadTypeNameList( globalOptions, "build_property.MetalamaMutableTypes" ),
                SymbolFacts.ReadTypeNameList( globalOptions, "build_property.MetalamaImmutableContractTypes" ) );
        }

        /// <summary>
        /// Gets the type names declared by the <c>MetalamaImmutableType</c> item, so that the analyzer can report a
        /// name that matches no type in the compilation.
        /// </summary>
        public IEnumerable<string> ImmutableTypeNames => this._additionalImmutableTypes;

        /// <summary>
        /// Gets the type names declared by the <c>MetalamaMutableType</c> item.
        /// </summary>
        public IEnumerable<string> MutableTypeNames => this._additionalMutableTypes;

        /// <summary>
        /// Gets the type names declared by the <c>MetalamaImmutableContractType</c> item.
        /// </summary>
        public IEnumerable<string> ContractTypeNames => this._additionalContractTypes;

        /// <summary>
        /// Reads the <c>bool</c> argument of the marker on a symbol, and indicates whether the marker is present.
        /// </summary>
        public static bool TryGetImmutableObjectValue( ISymbol symbol, out bool value )
        {
            foreach ( var attribute in symbol.GetAttributes() )
            {
                if ( attribute.AttributeClass is { } attributeClass
                     && attributeClass.Name == "ImmutableObjectAttribute"
                     && SymbolFacts.GetFullMetadataName( attributeClass ) == ImmutableObjectAttributeMetadataName )
                {
                    // The single constructor takes the bool. A call with anything else does not compile, but an
                    // analyzer must not assume that the code it sees does.
                    value = attribute.ConstructorArguments.Length == 1
                            && attribute.ConstructorArguments[0].Value is true;

                    return true;
                }
            }

            value = false;

            return false;
        }

        /// <summary>
        /// Determines whether a type declares that it waives the contract, that is, whether it carries
        /// <c>[ImmutableObject(false)]</c> directly.
        /// </summary>
        public static bool HasWaiver( INamedTypeSymbol type )
            => TryGetImmutableObjectValue( type, out var value ) && !value;

        /// <summary>
        /// Determines whether a type is bound by the immutability contract.
        /// </summary>
        /// <remarks>
        /// The nearest declaration wins, so <c>[ImmutableObject(false)]</c> on a class is the per-class opt-out even
        /// though its base type or an interface it implements requires immutability. That is the natural reading of
        /// the marker's own <c>bool</c> parameter, and unlike a <c>#pragma</c> it is greppable, survives refactoring
        /// and appears in the declaration.
        /// </remarks>
        public bool IsSubjectToContract( INamedTypeSymbol type )
        {
            if ( this._isSubjectToContract.TryGetValue( type, out var cached ) )
            {
                return cached;
            }

            var result = this.ComputeIsSubjectToContract( type );
            this._isSubjectToContract.TryAdd( type, result );

            return result;
        }

        private bool ComputeIsSubjectToContract( INamedTypeSymbol type )
        {
            // 1. The declaration on the type itself decides, in both directions.
            if ( TryGetImmutableObjectValue( type, out var own ) )
            {
                return own;
            }

            // 2. The nearest base type that declares anything decides.
            for ( var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType )
            {
                if ( TryGetImmutableObjectValue( baseType, out var inherited ) )
                {
                    return inherited;
                }

                if ( this.IsContractType( baseType ) )
                {
                    return true;
                }
            }

            // 3. Any interface that requires immutability binds the implementation. This is what makes marking
            //    IAspect sufficient to check every aspect that anyone writes.
            foreach ( var interfaceType in type.AllInterfaces )
            {
                if ( (TryGetImmutableObjectValue( interfaceType, out var fromInterface ) && fromInterface)
                     || this.IsContractType( interfaceType ) )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a type binds its implementations to the contract without declaring the marker, because
        /// it is named in the built-in table or by the <c>MetalamaImmutableContractType</c> item.
        /// </summary>
        private bool IsContractType( INamedTypeSymbol type )
        {
            var metadataName = SymbolFacts.GetFullMetadataName( type.OriginalDefinition );

            return WellKnownImmutabilityContractTypes.Contains( metadataName )
                   || this._additionalContractTypes.Contains( metadataName );
        }

        /// <summary>
        /// Evaluates the immutability of a type.
        /// </summary>
        public ImmutabilityVerdict GetVerdict( ITypeSymbol? type )
        {
            if ( type == null )
            {
                return ImmutabilityVerdict.Immutable;
            }

            if ( this._verdicts.TryGetValue( type, out var cached ) )
            {
                return cached;
            }

            var verdict = this.GetVerdictCore( type, 0 );

            // The function is pure, so a racing duplicate computation is harmless and TryAdd is preferred over
            // GetOrAdd with a recursive value factory.
            this._verdicts.TryAdd( type, verdict );

            return verdict;
        }

        private ImmutabilityVerdict GetVerdictCore( ITypeSymbol type, int depth )
        {
            if ( depth > _maxDepth )
            {
                // Silence is preferable to a chain that was cut short and would mislead.
                return ImmutabilityVerdict.Immutable;
            }

            // Rule 0. Never report on code that does not compile.
            if ( type.TypeKind == TypeKind.Error || type is IErrorTypeSymbol )
            {
                return ImmutabilityVerdict.Immutable;
            }

            // Rule 1. The intrinsics, copied from ImmutabilityExtensions.GetImmutabilityKind. The list is deliberately
            // the same one, and not a longer one: DateTime, IntPtr and UIntPtr are covered by rule 14 instead, which
            // is how the patterns implementation reaches them too.
            switch ( type.SpecialType )
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Void:
                    return ImmutabilityVerdict.Immutable;
            }

            // Rule 2. A delegate cannot be retargeted, an enumeration is a value, and a pointer is not an object.
            //
            // The delegate case is the sharpest disagreement with WellKnownDurableTypes, which rejects a delegate
            // because it holds its target and its closure. Both verdicts are right for their own question.
            if ( type.TypeKind is TypeKind.Delegate or TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer )
            {
                return ImmutabilityVerdict.Immutable;
            }

            // Rule 3. A nullable value type is exactly its underlying type, and adds no step to the chain. The
            // patterns implementation reaches Nullable<T> through the blanket rule for value types of namespace
            // System and therefore trusts it whatever T is; inspecting T costs one line and is strictly sounder.
            if ( type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType )
            {
                return this.GetVerdictCore( nullableType.TypeArguments[0], depth + 1 );
            }

            // Rule 4. A tuple is immutable when each of its elements is. A faithful port of the patterns rules would
            // classify every tuple as mutable, because ValueTuple is excluded from the blanket rule and is not a
            // readonly struct. As the type of a read-only field a tuple cannot be reassigned, so that would be a
            // false positive on almost every use.
            if ( type is INamedTypeSymbol { IsTupleType: true } tupleType )
            {
                foreach ( var element in tupleType.TupleElements )
                {
                    var elementVerdict = this.GetVerdictCore( element.Type, depth + 1 );

                    if ( !elementVerdict.IsImmutable )
                    {
                        return elementVerdict.Prepend( "." + element.Name );
                    }
                }

                return ImmutabilityVerdict.Immutable;
            }

            // Rule 5. An array is never immutable: a read-only field typed as an array still lets every element be
            // replaced. This is the second disagreement with the durability rules, which recurse into the element
            // type instead.
            if ( type is IArrayTypeSymbol )
            {
                return ImmutabilityVerdict.NotImmutable(
                    SymbolFacts.GetDisplayName( type ),
                    "an array is mutable: its elements can be replaced. Use ImmutableArray<T>" );
            }

            // Rule 6. A type parameter is immutable inside the definition. The obligation moves to the construction
            // site, where the type argument is known.
            if ( type.TypeKind == TypeKind.TypeParameter )
            {
                return ImmutabilityVerdict.Immutable;
            }

            // Rule 7. The static type says nothing about what may be stored.
            if ( type.SpecialType == SpecialType.System_Object || type is IDynamicTypeSymbol )
            {
                return ImmutabilityVerdict.NotImmutable(
                    type.Name.Length > 0 ? type.Name : "dynamic",
                    "the static type does not constrain what may be stored" );
            }

            if ( type is not INamedTypeSymbol namedType )
            {
                return ImmutabilityVerdict.Immutable;
            }

            var definition = namedType.OriginalDefinition;
            var metadataName = SymbolFacts.GetFullMetadataName( definition );

            // Rules 9 and 10. The project may override the verdict of a type it does not own. The mutable list wins.
            if ( this._additionalMutableTypes.Contains( metadataName ) )
            {
                return ImmutabilityVerdict.NotImmutable(
                    SymbolFacts.GetDisplayName( type ),
                    "the project declares this type in MetalamaMutableType" );
            }

            if ( this._additionalImmutableTypes.Contains( metadataName ) )
            {
                return ImmutabilityVerdict.Immutable;
            }

            // Rule 11. An exact match in the built-in table, tested before the walk of the base types below, because
            // a type may derive from a mutable one and still be immutable itself.
            if ( WellKnownImmutableTypes.TryGet( metadataName, out var entry ) )
            {
                switch ( entry.Immutability )
                {
                    case WellKnownImmutability.Immutable:
                        return ImmutabilityVerdict.Immutable;

                    case WellKnownImmutability.NotImmutable:
                        return ImmutabilityVerdict.NotImmutable( SymbolFacts.GetDisplayName( type ), entry.Reason );

                    case WellKnownImmutability.Transparent:
                        return this.GetTransparentVerdict( namedType, entry, depth );
                }
            }

            // Rule 12. A type that waives the contract is mutable wherever it is used, which is the point of the
            // waiver. Stated explicitly so that the message says so, rather than falling through to rule 17 and
            // reporting that the type is not marked when in fact it is.
            if ( HasWaiver( definition ) )
            {
                return ImmutabilityVerdict.NotImmutable(
                    SymbolFacts.GetDisplayName( type ),
                    "the type waives the contract with [ImmutableObject(false)]" );
            }

            // Rule 13. The declaration is trusted here. It is verified separately, by the rule that walks the members
            // of every type bound by the contract. A constructed generic is trusted only for the type arguments it
            // does not store, so that a phantom parameter costs nothing and Box<StringBuilder> is still reported
            // however well Box<T> itself satisfies its contract.
            if ( this.IsSubjectToContract( namedType ) )
            {
                if ( namedType.IsGenericType && !namedType.TypeArguments.IsDefaultOrEmpty )
                {
                    var storedParameters = this.GetStoredTypeParameters( definition );

                    for ( var i = 0; i < namedType.TypeArguments.Length && i < 64; i++ )
                    {
                        if ( (storedParameters & (1UL << i)) == 0 )
                        {
                            continue;
                        }

                        var argumentVerdict = this.GetVerdictCore( namedType.TypeArguments[i], depth + 1 );

                        if ( !argumentVerdict.IsImmutable )
                        {
                            return argumentVerdict.Prepend( SymbolFacts.GetDisplayName( type ) );
                        }
                    }
                }

                return ImmutabilityVerdict.Immutable;
            }

            // Rule 14. A type that derives from or implements a well-known mutable type is mutable too. This is what
            // classifies every symbol interface, every syntax node and every collection derived from List<T> without
            // listing them.
            var inherited = this.GetInheritedMutableVerdict( namedType, type );

            if ( inherited != null )
            {
                return inherited;
            }

            // Rule 15. A value type of namespace System is trusted, minus the names the patterns implementation
            // excludes. This is the widest rule of the table and it is kept as it is, deliberately, so that the two
            // implementations stay comparable. The known hole, ArraySegment<T>, is closed by a table entry above.
            if ( namedType.IsValueType
                 && namedType.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
                 && containingNamespace.ToDisplayString() == "System"
                 && !WellKnownImmutableTypes.NonImmutableSystemValueTypeNames.Contains( definition.Name ) )
            {
                return ImmutabilityVerdict.Immutable;
            }

            // Rule 16. A readonly struct cannot be reassigned through one of its own fields, but a field of it that
            // is typed as a mutable class still reaches a mutable object. The contract is deep, so this is reported,
            // with its own kind so that the message can explain the distinction.
            if ( namedType.IsReadOnly )
            {
                return ImmutabilityVerdict.ShallowOnly(
                    SymbolFacts.GetDisplayName( type ),
                    "a readonly struct is only shallowly immutable; its fields may reference mutable objects. "
                    + "Mark it [ImmutableObject(true)] to have that verified" );
            }

            // Rule 17. An interface or an abstract type has no members of its own to examine, so marking it does not
            // check anything here; it requires every implementation to be immutable, which the rule that walks the
            // members of a type bound by the contract then verifies. That is a different remedy from marking a class,
            // so it carries its own diagnostic.
            if ( namedType.TypeKind == TypeKind.Interface || namedType.IsAbstract )
            {
                return ImmutabilityVerdict.NotAnnotated(
                    SymbolFacts.GetDisplayName( type ),
                    "an interface or abstract type that is not marked [ImmutableObject(true)]" );
            }

            // Rule 18. Immutability is opt-in.
            return ImmutabilityVerdict.NotImmutable(
                SymbolFacts.GetDisplayName( type ),
                "the type is not marked [ImmutableObject(true)]" );
        }

        /// <summary>
        /// Evaluates a type whose immutability follows that of the type arguments selected by its mask.
        /// </summary>
        private ImmutabilityVerdict GetTransparentVerdict( INamedTypeSymbol type, WellKnownImmutabilityEntry entry, int depth )
        {
            var typeArguments = type.TypeArguments;

            for ( var i = 0; i < typeArguments.Length; i++ )
            {
                if ( !entry.ArgumentMask.IsDefault && !entry.ArgumentMask.Contains( i ) )
                {
                    continue;
                }

                var argumentVerdict = this.GetVerdictCore( typeArguments[i], depth + 1 );

                if ( !argumentVerdict.IsImmutable )
                {
                    return argumentVerdict.Prepend( SymbolFacts.GetDisplayName( type ) );
                }
            }

            return ImmutabilityVerdict.Immutable;
        }

        /// <summary>
        /// Returns the verdict of the first well-known mutable base type or interface of a type, or <c>null</c>.
        /// </summary>
        private ImmutabilityVerdict? GetInheritedMutableVerdict( INamedTypeSymbol type, ITypeSymbol reported )
        {
            for ( var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType )
            {
                if ( WellKnownImmutableTypes.TryGet( SymbolFacts.GetFullMetadataName( baseType.OriginalDefinition ), out var baseEntry )
                     && baseEntry.Immutability == WellKnownImmutability.NotImmutable )
                {
                    return ImmutabilityVerdict.NotImmutable( SymbolFacts.GetDisplayName( reported ), baseEntry.Reason );
                }
            }

            foreach ( var interfaceType in type.AllInterfaces )
            {
                if ( WellKnownImmutableTypes.TryGet(
                         SymbolFacts.GetFullMetadataName( interfaceType.OriginalDefinition ),
                         out var interfaceEntry )
                     && interfaceEntry.Immutability == WellKnownImmutability.NotImmutable )
                {
                    return ImmutabilityVerdict.NotImmutable( SymbolFacts.GetDisplayName( reported ), interfaceEntry.Reason );
                }
            }

            return null;
        }

        private ulong GetStoredTypeParameters( INamedTypeSymbol definition )
        {
            if ( this._storedTypeParameters.TryGetValue( definition, out var cached ) )
            {
                return cached;
            }

            var result = SymbolFacts.ComputeStoredTypeParameters( definition, _maxDepth );
            this._storedTypeParameters.TryAdd( definition, result );

            return result;
        }
    }
}
