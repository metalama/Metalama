// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// Decides whether a type is durable, for one compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every mutable field of this analyzer lives here, and an instance of this class is created inside
    /// <c>RegisterCompilationStartAction</c> and captured only by the actions registered on that context.</b> Roslyn
    /// keeps one instance of a <see cref="DiagnosticAnalyzer"/> alive for the lifetime of the process, so a cache of
    /// symbols held in a field of the analyzer would retain the compilation those symbols came from. That is the very
    /// defect this analyzer exists to report. Note by contrast that <c>ProjectClassifier</c> in the sibling
    /// <c>Metalama.Framework.Engine.Analyzers</c> project does hold a static cache, and is safe only because both its
    /// key and its value are strings.
    /// </para>
    /// <para>
    /// No cycle can arise while evaluating a type, because the evaluation never descends into the members of a type:
    /// a type is durable when it is marked or well known, and is otherwise not durable, so the recursion follows only
    /// array elements, type arguments and tuple elements. Those form a finite tree. The budget below therefore guards
    /// against pathological nesting rather than against non-termination.
    /// </para>
    /// </remarks>
    internal sealed partial class DurabilityContext
    {
        private const int _maxDepth = 20;

        /// <summary>
        /// The full metadata name of the attribute, matched by name because this project deliberately does not
        /// reference <c>Metalama.Framework</c>.
        /// </summary>
        public const string DurableAttributeMetadataName = "Metalama.Framework.Utilities.DurableAttribute";

        private readonly ConcurrentDictionary<ITypeSymbol, DurabilityVerdict> _verdicts;
        private readonly ConcurrentDictionary<INamedTypeSymbol, bool> _isSubjectToContract;
        private readonly ConcurrentDictionary<INamedTypeSymbol, StoredTypeParameters> _storedTypeParameters;
        private readonly HashSet<string> _additionalDurableTypes;
        private readonly HashSet<string> _additionalNonDurableTypes;

        private DurabilityContext(
            HashSet<string> additionalDurableTypes,
            HashSet<string> additionalNonDurableTypes )
        {
            this._verdicts = new ConcurrentDictionary<ITypeSymbol, DurabilityVerdict>( SymbolEqualityComparer.Default );
            this._isSubjectToContract = new ConcurrentDictionary<INamedTypeSymbol, bool>( SymbolEqualityComparer.Default );
            this._storedTypeParameters = new ConcurrentDictionary<INamedTypeSymbol, StoredTypeParameters>( SymbolEqualityComparer.Default );
            this._additionalDurableTypes = additionalDurableTypes;
            this._additionalNonDurableTypes = additionalNonDurableTypes;
        }

        /// <summary>
        /// Creates the context of a compilation, or returns <c>null</c> when the compilation does not know the
        /// <c>Durable</c> attribute, in which case no action is registered and the analyzer costs one failed symbol
        /// lookup.
        /// </summary>
        public static DurabilityContext? TryCreate( Compilation compilation, AnalyzerOptions options )
        {
            if ( compilation.GetTypeByMetadataName( DurableAttributeMetadataName ) == null )
            {
                return null;
            }

            var globalOptions = options.AnalyzerConfigOptionsProvider.GlobalOptions;

            return new DurabilityContext(
                SymbolFacts.ReadTypeNameList( globalOptions, "build_property.MetalamaDurableTypes" ),
                SymbolFacts.ReadTypeNameList( globalOptions, "build_property.MetalamaNonDurableTypes" ) );
        }

        /// <summary>
        /// Gets the type names declared by the <c>MetalamaDurableType</c> item, so that the analyzer can report a name
        /// that matches no type in the compilation.
        /// </summary>
        public IEnumerable<string> DurableTypeNames => this._additionalDurableTypes;

        /// <summary>
        /// Gets the type names declared by the <c>MetalamaNonDurableType</c> item.
        /// </summary>
        public IEnumerable<string> NonDurableTypeNames => this._additionalNonDurableTypes;

        /// <summary>
        /// Determines whether a type is bound by the durable contract, either because it carries the attribute or
        /// because it derives from or implements a type that does.
        /// </summary>
        /// <remarks>
        /// The contract propagates to implementations on purpose. <c>IDesignTimePipelineResultExtension</c> states it
        /// on a public interface, and an implementation that did not inherit the obligation would make the
        /// declaration worthless.
        /// </remarks>
        public bool IsSubjectToContract( INamedTypeSymbol type )
        {
            if ( this._isSubjectToContract.TryGetValue( type, out var result ) )
            {
                return result;
            }

            result = HasDurableAttribute( type );

            if ( !result )
            {
                for ( var baseType = type.BaseType; baseType != null && !result; baseType = baseType.BaseType )
                {
                    result = HasDurableAttribute( baseType );
                }
            }

            if ( !result )
            {
                foreach ( var interfaceType in type.AllInterfaces )
                {
                    if ( HasDurableAttribute( interfaceType ) )
                    {
                        result = true;

                        break;
                    }
                }
            }

            this._isSubjectToContract.TryAdd( type, result );

            return result;
        }

        /// <summary>
        /// Determines whether a symbol carries the <c>Durable</c> attribute.
        /// </summary>
        public static bool HasDurableAttribute( ISymbol symbol )
        {
            foreach ( var attribute in symbol.GetAttributes() )
            {
                if ( attribute.AttributeClass is { } attributeClass
                     && attributeClass.Name == "DurableAttribute"
                     && GetFullMetadataName( attributeClass ) == DurableAttributeMetadataName )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evaluates the durability of a type.
        /// </summary>
        public DurabilityVerdict GetVerdict( ITypeSymbol? type )
        {
            if ( type == null )
            {
                return DurabilityVerdict.Durable;
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

        private DurabilityVerdict GetVerdictCore( ITypeSymbol type, int depth )
        {
            if ( depth > _maxDepth )
            {
                // Silence is preferable to a chain that was cut short and would mislead.
                return DurabilityVerdict.Durable;
            }

            // Rule 0. Never report on code that does not compile.
            if ( type.TypeKind == TypeKind.Error || type is IErrorTypeSymbol )
            {
                return DurabilityVerdict.Durable;
            }

            // Rules 1 to 3. Intrinsics, enumerations and pointers reach nothing.
            switch ( type.SpecialType )
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_DateTime:
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Void:
                    return DurabilityVerdict.Durable;
            }

            if ( type.TypeKind is TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer )
            {
                return DurabilityVerdict.Durable;
            }

            // Rule 4. A nullable value type is exactly its underlying type, and adds no step to the chain.
            if ( type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType )
            {
                return this.GetVerdictCore( nullableType.TypeArguments[0], depth + 1 );
            }

            // Rule 5. An array is durable when its elements are.
            if ( type is IArrayTypeSymbol arrayType )
            {
                return this.GetVerdictCore( arrayType.ElementType, depth + 1 ).Prepend( "[]" );
            }

            // Rule 6. A type parameter is durable inside the definition. The obligation moves to the construction
            // site, where the type argument is known.
            if ( type.TypeKind == TypeKind.TypeParameter )
            {
                return DurabilityVerdict.Durable;
            }

            // Rule 7. A tuple is durable when each of its elements is.
            if ( type is INamedTypeSymbol { IsTupleType: true } tupleType )
            {
                foreach ( var element in tupleType.TupleElements )
                {
                    var elementVerdict = this.GetVerdictCore( element.Type, depth + 1 );

                    if ( !elementVerdict.IsDurable )
                    {
                        return elementVerdict.Prepend( "." + element.Name );
                    }
                }

                return DurabilityVerdict.Durable;
            }

            // Rule 8. The static type says nothing about what may be stored.
            if ( type.SpecialType == SpecialType.System_Object || type is IDynamicTypeSymbol )
            {
                return DurabilityVerdict.NotDurable(
                    type.Name.Length > 0 ? type.Name : "dynamic",
                    "the static type does not constrain what may be stored" );
            }

            // Rule 9. A delegate holds its target and its closure, and the closure is invisible in the source.
            if ( type.TypeKind == TypeKind.Delegate )
            {
                return DurabilityVerdict.NotDurable(
                    GetDisplayName( type ),
                    "a delegate holds its target and everything its closure captured" );
            }

            if ( type is not INamedTypeSymbol namedType )
            {
                return DurabilityVerdict.Durable;
            }

            var definition = namedType.OriginalDefinition;
            var metadataName = GetFullMetadataName( definition );

            // Rule 10. The project may override the verdict of a type it does not own. The non-durable list wins.
            if ( this._additionalNonDurableTypes.Contains( metadataName ) )
            {
                return DurabilityVerdict.NotDurable( GetDisplayName( type ), "the project declares this type in MetalamaNonDurableType" );
            }

            if ( this._additionalDurableTypes.Contains( metadataName ) )
            {
                return DurabilityVerdict.Durable;
            }

            // Rule 11. An exact match in the built-in tables. This is tested before the walk of the base types below,
            // because a type may derive from one that is not durable and still be durable itself: IDurableRef derives
            // from IRef, and is the whole point of the distinction.
            if ( WellKnownDurableTypes.TryGet( metadataName, out var entry ) )
            {
                switch ( entry.Durability )
                {
                    case WellKnownDurability.Durable:
                        return DurabilityVerdict.Durable;

                    case WellKnownDurability.NotDurable:
                        return DurabilityVerdict.NotDurable( GetDisplayName( type ), entry.Reason );

                    case WellKnownDurability.Transparent:
                        return this.GetTransparentVerdict( namedType, entry, depth );
                }
            }

            // Rule 12. A type that derives from or implements a well-known non-durable type is not durable either.
            // This is what classifies every symbol interface, every syntax node and every code model declaration
            // without listing them.
            var inherited = this.GetInheritedNonDurableVerdict( namedType, type );

            if ( inherited != null )
            {
                return inherited;
            }

            // Rule 13. The declaration is trusted here. It is verified separately, by the rule that walks the members
            // of every type bound by the contract. A constructed generic is trusted only for the type arguments it
            // does not store: DurableLazy<T> holds a T, so DurableLazy<Compilation> is not durable however well
            // DurableLazy<T> itself satisfies its contract.
            if ( this.IsSubjectToContract( namedType ) )
            {
                if ( namedType.IsGenericType && !namedType.TypeArguments.IsDefaultOrEmpty )
                {
                    var storedParameters = this.GetStoredTypeParameters( definition );

                    for ( var i = 0; i < namedType.TypeArguments.Length; i++ )
                    {
                        if ( !storedParameters.IsStored( i ) )
                        {
                            continue;
                        }

                        var argumentVerdict = this.GetVerdictCore( namedType.TypeArguments[i], depth + 1 );

                        if ( !argumentVerdict.IsDurable )
                        {
                            return argumentVerdict.Prepend( GetDisplayName( type ) );
                        }
                    }
                }

                return DurabilityVerdict.Durable;
            }

            // Rule 14. An interface or an abstract type has no members of its own to examine, so marking it does not
            // check anything here; it requires every implementation to be durable, which the rule that walks the
            // members of a type bound by the contract then verifies. That is a different remedy from marking a class,
            // so it carries its own diagnostic.
            if ( namedType.TypeKind == TypeKind.Interface || namedType.IsAbstract )
            {
                return DurabilityVerdict.NotAnnotated(
                    GetDisplayName( type ),
                    "an interface or abstract type that is not marked [Durable]" );
            }

            // Rule 15. Durability is opt-in.
            return DurabilityVerdict.NotDurable( GetDisplayName( type ), "the type is not marked [Durable]" );
        }

        /// <summary>
        /// Evaluates a type whose durability follows that of the type arguments selected by its mask.
        /// </summary>
        private DurabilityVerdict GetTransparentVerdict( INamedTypeSymbol type, WellKnownEntry entry, int depth )
        {
            var typeArguments = type.TypeArguments;

            for ( var i = 0; i < typeArguments.Length; i++ )
            {
                if ( !entry.ArgumentMask.IsDefault && !entry.ArgumentMask.Contains( i ) )
                {
                    continue;
                }

                var argumentVerdict = this.GetVerdictCore( typeArguments[i], depth + 1 );

                if ( !argumentVerdict.IsDurable )
                {
                    return argumentVerdict.Prepend( GetDisplayName( type ) );
                }
            }

            return DurabilityVerdict.Durable;
        }

        /// <summary>
        /// Returns the verdict of the first well-known non-durable base type or interface of a type, or <c>null</c>.
        /// </summary>
        private DurabilityVerdict? GetInheritedNonDurableVerdict( INamedTypeSymbol type, ITypeSymbol reported )
        {
            for ( var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType )
            {
                if ( WellKnownDurableTypes.TryGet( GetFullMetadataName( baseType.OriginalDefinition ), out var baseEntry )
                     && baseEntry.Durability == WellKnownDurability.NotDurable )
                {
                    return DurabilityVerdict.NotDurable( GetDisplayName( reported ), baseEntry.Reason );
                }
            }

            foreach ( var interfaceType in type.AllInterfaces )
            {
                if ( WellKnownDurableTypes.TryGet( GetFullMetadataName( interfaceType.OriginalDefinition ), out var interfaceEntry )
                     && interfaceEntry.Durability == WellKnownDurability.NotDurable )
                {
                    return DurabilityVerdict.NotDurable( GetDisplayName( reported ), interfaceEntry.Reason );
                }
            }

            return null;
        }

        /// <summary>
        /// Returns, as a bit per ordinal, the type parameters of a generic definition that appear in the type of one
        /// of its instance fields, and which a construction of that type must therefore supply durably.
        /// </summary>
        /// <remarks>
        /// The computation is in <see cref="SymbolFacts.ComputeStoredTypeParameters"/>, which the immutability
        /// contract of this same assembly needs for the same reason. The cache stays here, because it holds symbols
        /// and must therefore die with the compilation.
        /// </remarks>
        private StoredTypeParameters GetStoredTypeParameters( INamedTypeSymbol definition )
        {
            if ( this._storedTypeParameters.TryGetValue( definition, out var cached ) )
            {
                return cached;
            }

            var result = SymbolFacts.ComputeStoredTypeParameters( definition, _maxDepth );

            this._storedTypeParameters.TryAdd( definition, result );

            return result;
        }

        /// <summary>
        /// Returns the name of a type as it appears in a diagnostic message.
        /// </summary>
        public static string GetDisplayName( ITypeSymbol type ) => SymbolFacts.GetDisplayName( type );

        /// <summary>
        /// Returns the full metadata name of a type, that is, the name by which the tables and the
        /// <c>MetalamaDurableType</c> items refer to it: the namespace, the chain of containing types separated by
        /// <c>+</c>, and the name of the type with its arity.
        /// </summary>
        public static string GetFullMetadataName( INamedTypeSymbol type ) => SymbolFacts.GetFullMetadataName( type );
    }
}
