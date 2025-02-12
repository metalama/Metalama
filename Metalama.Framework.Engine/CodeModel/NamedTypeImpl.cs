// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Accessibility = Metalama.Framework.Code.Accessibility;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel;

internal sealed class NamedTypeImpl : MemberOrNamedType, INamedTypeImpl
{
    private readonly NamedType _facade;

    ITypeSymbol ISdkType.TypeSymbol => this.TypeSymbol;

    public override ISymbol Symbol => this.TypeSymbol;

    public override bool CanBeInherited => this.IsReferenceType.GetValueOrDefault() && !this.IsSealed;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
        => this.Compilation.GetDerivedTypes( this, options );

    internal NamedTypeImpl( NamedType facade, INamedTypeSymbol typeSymbol, CompilationModel compilation ) : base( compilation )
    {
        this._facade = facade;
        this.TypeSymbol = typeSymbol;
    }

    TypeKind IType.TypeKind
        => this.TypeSymbol.TypeKind switch
        {
            Microsoft.CodeAnalysis.TypeKind.Class when !this.TypeSymbol.IsRecord => TypeKind.Class,
            Microsoft.CodeAnalysis.TypeKind.Class when this.TypeSymbol.IsRecord => TypeKind.RecordClass,
            Microsoft.CodeAnalysis.TypeKind.Delegate => TypeKind.Delegate,
            Microsoft.CodeAnalysis.TypeKind.Enum => TypeKind.Enum,
            Microsoft.CodeAnalysis.TypeKind.Interface => TypeKind.Interface,
            Microsoft.CodeAnalysis.TypeKind.Struct when !this.TypeSymbol.IsRecord => TypeKind.Struct,
            Microsoft.CodeAnalysis.TypeKind.Struct when this.TypeSymbol.IsRecord => TypeKind.RecordStruct,
            Microsoft.CodeAnalysis.TypeKind.Error => TypeKind.Error,
            _ => throw new InvalidOperationException( $"Unexpected type kind for '{this.TypeSymbol}': {this.TypeSymbol.TypeKind}." )
        };

    [Memo]
    public SpecialType SpecialType => this.GetSpecialTypeCore();

    private SpecialType GetSpecialTypeCore()
    {
        var specialType = this.TypeSymbol.SpecialType.ToOurSpecialType();

        if ( specialType != SpecialType.None )
        {
            return specialType;
        }
        else if ( this.IsGeneric )
        {
            switch ( this.TypeSymbol.Name )
            {
                case "IAsyncEnumerable" when this.IsCanonicalGenericInstance
                                             && this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System.Collections.Generic":
                    return SpecialType.IAsyncEnumerable_T;

                case "IAsyncEnumerator" when this.IsCanonicalGenericInstance
                                             && this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System.Collections.Generic":
                    return SpecialType.IAsyncEnumerator_T;

                case nameof(ValueTask)
                    when this.IsCanonicalGenericInstance && this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks":
                    return SpecialType.ValueTask_T;

                case nameof(Task) when this.IsCanonicalGenericInstance && this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks":
                    return SpecialType.Task_T;
            }

            return SpecialType.None;
        }
        else
        {
            return this.TypeSymbol.Name switch
            {
                nameof(ValueTask) when this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
                    => SpecialType.ValueTask,
                nameof(Task) when this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
                    => SpecialType.Task,
                nameof(Type) when this.TypeSymbol.ContainingNamespace.ToDisplayString() == "System"
                    => SpecialType.Type,
                _ => SpecialType.None
            };
        }
    }

    public Type ToType() => this.GetCompilationModel().Factory.GetReflectionType( this.TypeSymbol );

    public bool? IsReferenceType => this.TypeSymbol.IsReferenceType;

    public bool? IsNullable => this.TypeSymbol.IsNullable();

    public bool Equals( SpecialType specialType ) => this.SpecialType == specialType;

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this._facade, otherType );

    public override MemberInfo ToMemberInfo() => this.ToType();

    public bool IsReadOnly => this.TypeSymbol.IsReadOnly;

    public bool IsRef => this.TypeSymbol.IsRefLikeType;

    public bool HasDefaultConstructor
        => this.TypeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Struct ||
           (this.TypeSymbol is { TypeKind: Microsoft.CodeAnalysis.TypeKind.Class, IsAbstract: false } &&
            this.TypeSymbol.InstanceConstructors.Any( ctor => ctor.Parameters.Length == 0 ));

    public bool IsGeneric => this.TypeSymbol.IsGenericType;

    public bool IsCanonicalGenericInstance => this.TypeSymbol.OriginalDefinition == this.TypeSymbol;

    [Memo]
    public INamedTypeCollection Types
        => new NamedTypeCollection(
            this._facade,
            this.Compilation.GetNamedTypeCollection( this.TypeSymbol.ToValueTypedRef<INamespaceOrNamedType>( this.Compilation.CompilationContext ) ) );

    INamedTypeCollection INamedType.NestedTypes => this.Types;

    [Memo]
    public INamedTypeCollection AllTypes => new AllTypesCollection( this._facade );

    [Memo]
    public IPropertyCollection Properties
        => new PropertyCollection(
            this._facade,
            this.Compilation.GetPropertyCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) ) );

    [Memo]
    public IPropertyCollection AllProperties => new AllPropertiesCollection( this._facade );

    [Memo]
    public IIndexerCollection Indexers
        => new IndexerCollection(
            this._facade,
            this.Compilation.GetIndexerCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) ) );

    [Memo]
    public IIndexerCollection AllIndexers => new AllIndexersCollection( this._facade );

    [Memo]
    public IFieldCollection Fields
        => new FieldCollection(
            this._facade,
            this.Compilation.GetFieldCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) ) );

    [Memo]
    public IFieldCollection AllFields => new AllFieldsCollection( this._facade );

    [Memo]
    public IFieldOrPropertyCollection FieldsAndProperties => new FieldAndPropertiesCollection( this.Fields, this.Properties );

    [Memo]
    public IFieldOrPropertyCollection AllFieldsAndProperties => new AllFieldsAndPropertiesCollection( this._facade );

    [Memo]
    public IEventCollection Events
        => new EventCollection(
            this._facade,
            this.Compilation.GetEventCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) ) );

    [Memo]
    public IEventCollection AllEvents => new AllEventsCollection( this._facade );

    [Memo]
    public IMethodCollection Methods
        => new MethodCollection(
            this._facade,
            this.Compilation.GetMethodCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) ) );

    [Memo]
    public IMethodCollection AllMethods => new AllMethodsCollection( this._facade );

    [Memo]
    public IConstructorCollection Constructors
        => new ConstructorCollection(
            this._facade,
            this.Compilation.GetConstructorCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) ) );

    [Memo]
    public IConstructor? PrimaryConstructor => this.GetPrimaryConstructorImpl();

    [Memo]
    public IConstructor? StaticConstructor => this.GetStaticConstructorImpl();

    public IMethod? Finalizer => this.GetFinalizerImpl();

    private IConstructor? GetPrimaryConstructorImpl()
    {
        var constructors = this.Compilation.GetConstructorCollection( this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ) );

        foreach ( var constructor in constructors )
        {
            if ( constructor.Target is IMethodSymbol methodSymbol && methodSymbol.IsPrimaryConstructor() )
            {
                return this.Compilation.Factory.GetConstructor( methodSymbol );
            }

            // TODO: Builders? (In case we e.g. add a parameter)
        }

        return null;
    }

    private IConstructor? GetStaticConstructorImpl()
    {
        var builder = this.Compilation.GetStaticConstructor( this.TypeSymbol );

        if ( builder != null )
        {
            return this.Compilation.Factory.GetConstructor( builder );
        }

        var symbol = this.TypeSymbol.StaticConstructors.SingleOrDefault();

        if ( symbol != null )
        {
            return this.Compilation.Factory.GetConstructor( symbol );
        }

        return null;
    }

    private IMethod? GetFinalizerImpl()
    {
        var builder = this.Compilation.GetFinalizer( this.TypeSymbol );

        if ( builder != null )
        {
            return this.Compilation.Factory.GetDeclaration<IMethod>( builder );
        }

        var symbol = this.TypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .SingleOrDefault( m => m is { Name: "Finalize", TypeParameters.Length: 0, Parameters.Length: 0 } );

        if ( symbol != null )
        {
            return this.Compilation.Factory.GetFinalizer( symbol );
        }

        return null;
    }

    public bool IsPartial
    {
        get
        {
            var syntaxReference = this.TypeSymbol.GetPrimarySyntaxReference();

            if ( syntaxReference == null )
            {
                return false;
            }

            var syntax = syntaxReference.GetSyntax();

            var modifiers = syntax switch
            {
                TypeDeclarationSyntax type => type.Modifiers,
                EnumDeclarationSyntax e => e.Modifiers,
                DelegateDeclarationSyntax d => d.Modifiers,
                _ => default
            };

            return modifiers.Any( m => m.IsKind( SyntaxKind.PartialKeyword ) );
        }
    }

    [Memo]
    public IGenericParameterList TypeParameters
        => new TypeParameterList(
            this._facade,
            this.TypeSymbol.TypeParameters.Select( x => Ref.FromSymbol<ITypeParameter>( x, this.Compilation.CompilationContext ) )
                .ToReadOnlyList() );

    INamespace INamedType.Namespace => this.ContainingNamespace;

    [Memo]
    public INamespace ContainingNamespace
        =>

            // Empty error type symbols (like unspecified type parameter) have null namespace.
            // Other error types usually have assembly-specific global namespace.
            this.TypeSymbol.ContainingNamespace != null
                ? this.Compilation.Factory.GetNamespace( this.TypeSymbol.ContainingNamespace )
                : this.Compilation.GlobalNamespace;

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.BoxedRef;

    [Memo]
    public string FullName => this.TypeSymbol.GetFullName().AssertNotNull();

    [Memo]
    public IReadOnlyList<IType> TypeArguments => this.TypeSymbol.TypeArguments.Select( a => this.Compilation.Factory.GetIType( a ) ).ToImmutableList();

    [Memo]
    public override IDeclaration ContainingDeclaration
        => this.TypeSymbol.ContainingSymbol switch
        {
            INamespaceSymbol => this.Compilation.Factory.GetAssembly( this.TypeSymbol.ContainingAssembly ),
            INamedTypeSymbol containingType => this.Compilation.Factory.GetNamedType( containingType ),
            null => this.Compilation, // Empty error type symbol goes here. Other error types return a namespace, which we handle above.
            _ => throw new AssertionFailedException( $"Unexpected containing symbol kind: {this.TypeSymbol.ContainingSymbol.Kind}." )
        };

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    [Memo]
    public INamedType? BaseType => this.TypeSymbol.BaseType == null ? null : this.Compilation.Factory.GetNamedType( this.TypeSymbol.BaseType );

    [Memo]
    public IImplementedInterfaceCollection AllImplementedInterfaces
        => new AllImplementedInterfacesCollection(
            this._facade,
            this.Compilation.GetAllInterfaceImplementationCollection(
                this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ),
                false ) );

    [Memo]
    public IImplementedInterfaceCollection ImplementedInterfaces
        => new ImplementedInterfacesCollection(
            this._facade,
            this.Compilation.GetInterfaceImplementationCollection(
                this.TypeSymbol.ToValueTypedRef<INamedType>( this.Compilation.CompilationContext ),
                false ) );

    ICompilation ICompilationElement.Compilation => this.Compilation;

    IGeneric IGenericInternal.ConstructGenericInstance( IReadOnlyList<IType> typeArguments )
    {
        var typeArgumentSymbols =
            typeArguments.SelectAsArray( a => a.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.ConstructedIntroducedTypes ) );

        var typeSymbol = this.TypeSymbol;
        var constructedTypeSymbol = typeSymbol.Construct( typeArgumentSymbols );

        return this.Compilation.Factory.GetNamedType( constructedTypeSymbol );
    }

    public IReadOnlyList<IMember> GetOverridingMembers( IMember member )
    {
        var isInterfaceMember = member.DeclaringType.TypeKind == TypeKind.Interface;

        if ( !((IDeclarationImpl) member).CanBeInherited )
        {
            return Array.Empty<IMember>();
        }

        IMemberCollection<IMember> members;

        switch ( member.DeclarationKind )
        {
            case DeclarationKind.Method:
                members = this.Methods;

                break;

            case DeclarationKind.Property:
                members = this.Properties;

                break;

            case DeclarationKind.Event:
                members = this.Events;

                break;

            case DeclarationKind.Indexer:
                members = this.Indexers;

                break;

            default:
                return Array.Empty<IMember>();
        }

        var candidates = members.Where( m => m.GetCleanName() == member.Name );

        var overridingMembers = new List<IMember>();

        foreach ( var candidate in candidates )
        {
            if ( isInterfaceMember )
            {
                if ( ((INamedTypeImpl) candidate.DeclaringType).IsImplementationOfInterfaceMember( candidate, member ) )
                {
                    overridingMembers.Add( candidate );
                }
            }
            else
            {
                // Override. Look for overrides.
                for ( var c = (IMemberImpl) candidate; c != null; c = (IMemberImpl?) c.OverriddenMember )
                {
                    if ( c.OverriddenMember?.Definition == member )
                    {
                        overridingMembers.Add( candidate );
                    }
                }
            }
        }

        return overridingMembers;
    }

    public bool IsImplementationOfInterfaceMember( IMember typeMember, IMember interfaceMember )
    {
        // Some trivial checks first.
        if ( !typeMember.Name.EndsWith( interfaceMember.Name, StringComparison.Ordinal )
             || typeMember.DeclarationKind != interfaceMember.DeclarationKind
             || !(typeMember.Accessibility == Accessibility.Public || typeMember.IsExplicitInterfaceImplementation) )
        {
            return false;
        }

        var interfaceType = interfaceMember.DeclaringType.GetSymbol();
        var relevantInterfaces = this.GetAllInterfaces().Where( t => t.Equals( interfaceType ) || t.ConstructedFrom.Equals( interfaceType ) );

        foreach ( var implementedInterface in relevantInterfaces )
        {
            foreach ( var candidateSymbol in implementedInterface.GetMembers( interfaceMember.Name ) )
            {
                var candidateMember = (IMember) this.Compilation.Factory.GetDeclaration( candidateSymbol );

                if ( (candidateMember.SignatureEquals( interfaceMember )
                      || candidateMember.Definition.SignatureEquals( interfaceMember ))
                     && candidateMember.SignatureEquals( typeMember ) )
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsSubclassOf( INamedType type )
    {
        // TODO: enum.IsSubclassOf(int) == true etc.
        if ( type.TypeKind is TypeKind.Class or TypeKind.RecordClass )
        {
            INamedType? currentType = this;

            while ( currentType != null )
            {
                if ( this.Compilation.Comparers.Default.Equals( currentType, type ) )
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }
        else if ( type.TypeKind == TypeKind.Interface )
        {
            return this.ImplementedInterfaces.SingleOrDefault( i => this.Compilation.Comparers.Default.Equals( i, type ) ) != null;
        }
        else
        {
            return this.Compilation.Comparers.Default.Equals( this, type );
        }
    }

    private static IMember? FindMemberOfSignature( INamedType type, IMember member )
    {
        IMember? candidate = member switch
        {
            IMethod method => type.Methods.OfExactSignature( method ),
            IConstructor constructor => type.Constructors.OfExactSignature( constructor ),
            IProperty property => type.Properties.OfName( property.Name ).SingleOrDefault(),
            IIndexer indexer => type.Indexers.OfExactSignature( indexer ),
            IEvent @event => type.Events.OfName( @event.Name ).SingleOrDefault(),
            _ => throw new AssertionFailedException( $"Unexpected member kind: {member.DeclarationKind}." )
        };

        if ( StructuralDeclarationComparer.ContainingDeclarationOblivious.Equals( candidate, member ) )
        {
            return candidate;
        }

        return null;
    }

    public bool TryFindImplementationForInterfaceMember( IMember interfaceMember, [NotNullWhen( true )] out IMember? implementationMember )
    {
        // TODO: Type introductions.
        var symbolInterfaceMemberImplementationSymbol =
            this.TypeSymbol.FindImplementationForInterfaceMember(
                interfaceMember.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedInterfaceImplementation ) );

        var symbolInterfaceMemberImplementation =
            symbolInterfaceMemberImplementationSymbol != null
                ? (IMember) this.Compilation.Factory.GetDeclaration( symbolInterfaceMemberImplementationSymbol )
                : null;

        // Introduced implementation can be implementing the interface member in a subtype.
        INamedType? currentType = this;

        while ( currentType != null )
        {
            var introducedInterface =
                this.Compilation
                    .GetInterfaceImplementationCollection( currentType.ToValueTypedRef(), false )
                    .Introductions
                    .SingleOrDefault( i => this.Compilation.Comparers.Default.Equals( i.InterfaceType, interfaceMember.DeclaringType ) );

            if ( introducedInterface != null )
            {
                // TODO: Generics.
                if ( !introducedInterface.MemberMap.TryGetValue( interfaceMember, out var interfaceMemberImplementation ) )
                {
                    var candidate = FindMemberOfSignature( currentType, interfaceMember );

                    if ( candidate?.Accessibility == Accessibility.Public )
                    {
                        interfaceMemberImplementation = candidate;
                    }
                    else
                    {
                        implementationMember = null;

                        return false;
                    }
                }

                // Which is later in inheritance?
                if ( symbolInterfaceMemberImplementation == null || currentType.IsSubclassOf( symbolInterfaceMemberImplementation.DeclaringType ) )
                {
                    implementationMember = interfaceMemberImplementation.ForCompilation( this.Compilation );

                    return true;
                }
                else
                {
                    implementationMember = symbolInterfaceMemberImplementation.ForCompilation( this.Compilation );

                    return true;
                }
            }

            currentType = currentType.BaseType?.Definition;
        }

        if ( symbolInterfaceMemberImplementation != null )
        {
            implementationMember = symbolInterfaceMemberImplementation.ForCompilation( this.Compilation );

            return true;
        }
        else
        {
            implementationMember = null;

            return false;
        }
    }

    public INamedTypeSymbol TypeSymbol { get; }

    INamedType INamedType.Definition => throw new NotSupportedException();

    INamedType INamedType.TypeDefinition => throw new NotSupportedException();

    protected override IMemberOrNamedType GetDefinition() => throw new NotSupportedException();

    [Memo]
    public INamedType UnderlyingType => this.GetUnderlyingTypeCore();

    private INamedType GetUnderlyingTypeCore()
    {
        var enumUnderlyingType = this.TypeSymbol.EnumUnderlyingType;

        if ( enumUnderlyingType != null )
        {
            return this.Compilation.Factory.GetNamedType( enumUnderlyingType );
        }

        var isNullable = this.IsNullable;

        if ( isNullable != null )
        {
            if ( this.IsReferenceType == true )
            {
                // We have an annotated reference type, return the non-annotated type.
                return this.Compilation.Factory.GetNamedType( (INamedTypeSymbol) this.TypeSymbol.WithNullableAnnotation( NullableAnnotation.None ) );
            }
        }

        // Fall back to self.
        return this._facade;
    }

    private void PopulateAllInterfaces( ImmutableHashSet<INamedTypeSymbol>.Builder builder, GenericMap genericMap )
    {
        // Process the Roslyn type system.
        foreach ( var type in this.TypeSymbol.AllInterfaces )
        {
            builder.Add( genericMap.Map( type ) );
        }

        if ( this.TypeSymbol.BaseType != null )
        {
            var newGenericMap = genericMap.CreateBaseMap( this.TypeSymbol.BaseType.TypeArguments );
            ((NamedType) this.BaseType!).Implementation.PopulateAllInterfaces( builder, newGenericMap );
        }

        // TODO: process introductions.
    }

    private ImmutableHashSet<INamedTypeSymbol> GetAllInterfaces()
    {
        var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>( this.Compilation.CompilationContext.SymbolComparer );
        this.PopulateAllInterfaces( builder, this.Compilation.EmptyGenericMap );

        return builder.ToImmutable();
    }

    ITypeImpl ITypeImpl.Accept( TypeRewriter visitor ) => throw new NotSupportedException();

    public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

    public bool Equals( INamedType? other ) => this.Equals( other, TypeComparison.Default );

    public override int GetHashCode() => this.Compilation.CompilationContext.SymbolComparer.GetHashCode( this.TypeSymbol );

    [Memo]
    private BoxedRef<INamedType> BoxedRef => new( this.ToValueTypedRef() );

    private protected override IRef<IDeclaration> ToDeclarationRef() => this.BoxedRef;

    IRef<INamedType> INamedType.ToRef() => this.BoxedRef;

    IRef<IType> IType.ToRef() => this.BoxedRef;

    protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.BoxedRef;
}