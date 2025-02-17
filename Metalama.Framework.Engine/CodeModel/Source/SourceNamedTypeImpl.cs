// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
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

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class SourceNamedTypeImpl : SourceMemberOrNamedType, INamedTypeImpl
{
    private readonly SourceNamedType _facade;

    private readonly INamedTypeSymbol _namedTypeSymbol;

    public override ISymbol Symbol => this._namedTypeSymbol;

    public override bool CanBeInherited => this.IsReferenceType.GetValueOrDefault() && !this.IsSealed;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
        => this.Compilation.GetDerivedTypes( this, options );

    internal SourceNamedTypeImpl(
        SourceNamedType facade,
        INamedTypeSymbol namedTypeSymbol,
        CompilationModel compilation,
        GenericContext? genericContextForSymbolMapping ) : base( compilation, genericContextForSymbolMapping )
    {
        this._facade = facade;
        this._namedTypeSymbol = namedTypeSymbol.AssertBelongsToCompilationContext( compilation.CompilationContext );
    }

    TypeKind IType.TypeKind
        => this._namedTypeSymbol.TypeKind switch
        {
            Microsoft.CodeAnalysis.TypeKind.Class when !this._namedTypeSymbol.IsRecord => TypeKind.Class,
            Microsoft.CodeAnalysis.TypeKind.Class when this._namedTypeSymbol.IsRecord => TypeKind.RecordClass,
            Microsoft.CodeAnalysis.TypeKind.Delegate => TypeKind.Delegate,
            Microsoft.CodeAnalysis.TypeKind.Enum => TypeKind.Enum,
            Microsoft.CodeAnalysis.TypeKind.Interface => TypeKind.Interface,
            Microsoft.CodeAnalysis.TypeKind.Struct when !this._namedTypeSymbol.IsRecord => TypeKind.Struct,
            Microsoft.CodeAnalysis.TypeKind.Struct when this._namedTypeSymbol.IsRecord => TypeKind.RecordStruct,
            Microsoft.CodeAnalysis.TypeKind.Error => TypeKind.Error,
            _ => throw new InvalidOperationException( $"Unexpected type kind for '{this._namedTypeSymbol}': {this._namedTypeSymbol.TypeKind}." )
        };

    [Memo]
    public SpecialType SpecialType => this.GetSpecialTypeCore();

    private SpecialType GetSpecialTypeCore()
    {
        var specialType = this._namedTypeSymbol.SpecialType.ToOurSpecialType();

        if ( specialType != SpecialType.None )
        {
            return specialType;
        }
        else if ( this.IsGeneric )
        {
            switch ( this._namedTypeSymbol.Name )
            {
                case "IAsyncEnumerable" when this.IsCanonicalGenericInstance
                                             && this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Collections.Generic":
                    return SpecialType.IAsyncEnumerable_T;

                case "IAsyncEnumerator" when this.IsCanonicalGenericInstance
                                             && this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Collections.Generic":
                    return SpecialType.IAsyncEnumerator_T;

                case nameof(ValueTask)
                    when this.IsCanonicalGenericInstance && this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks":
                    return SpecialType.ValueTask_T;

                case nameof(Task)
                    when this.IsCanonicalGenericInstance && this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks":
                    return SpecialType.Task_T;
            }

            return SpecialType.None;
        }
        else
        {
            return this._namedTypeSymbol.Name switch
            {
                nameof(ValueTask) when this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
                    => SpecialType.ValueTask,
                nameof(Task) when this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
                    => SpecialType.Task,
                nameof(Type) when this._namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System"
                    => SpecialType.Type,
                _ => SpecialType.None
            };
        }
    }

    public Type ToType() => this.Compilation.Factory.GetReflectionType( this._namedTypeSymbol );

    public bool? IsReferenceType => this._namedTypeSymbol.IsReferenceType;

    public bool? IsNullable => this._namedTypeSymbol.IsNullable();

    public bool Equals( SpecialType specialType ) => this.SpecialType == specialType;

    public bool Equals( IType? otherType, TypeComparison typeComparison )
        => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this._facade, otherType );

    public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
        => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

    public override bool Equals( object? obj )
        => obj switch
        {
            IType otherType => this.Equals( otherType ),
            Type otherType => this.Equals( otherType ),
            _ => false
        };

    IArrayType IType.MakeArrayType( int rank ) => throw new NotImplementedException();

    IPointerType IType.MakePointerType() => throw new NotImplementedException();

    INamedType INamedType.ToNullable() => throw new NotImplementedException();

    IType IType.ToNullable() => throw new NotImplementedException();

    IType IType.ToNonNullable() => throw new NotImplementedException();

    public override MemberInfo ToMemberInfo() => this.ToType();

    public bool IsReadOnly => this._namedTypeSymbol.IsReadOnly;

    public bool IsRef => this._namedTypeSymbol.IsRefLikeType;

    public bool HasDefaultConstructor
        => this._namedTypeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Struct ||
           (this._namedTypeSymbol is { TypeKind: Microsoft.CodeAnalysis.TypeKind.Class, IsAbstract: false } &&
            this._namedTypeSymbol.InstanceConstructors.Any( ctor => ctor.Parameters.Length == 0 ));

    public bool IsGeneric => this._namedTypeSymbol.IsGenericType;

    public bool IsCanonicalGenericInstance
        => this._namedTypeSymbol.OriginalDefinition == this._namedTypeSymbol && this.GenericContextForSymbolMapping.IsEmptyOrIdentity;

    [Memo]
    public INamedTypeCollection Types
        => new NamedTypeCollection(
            this._facade,
            this.Compilation.GetNamedTypeCollectionByParent( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

    INamedTypeCollection INamedType.NestedTypes => this.Types;

    [Memo]
    public INamedTypeCollection AllTypes => new AllTypesCollection( this._facade );

    [Memo]
    public IPropertyCollection Properties
        => new PropertyCollection(
            this._facade,
            this.Compilation.GetPropertyCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

    [Memo]
    public IPropertyCollection AllProperties => new AllPropertiesCollection( this._facade );

    [Memo]
    public IIndexerCollection Indexers
        => new IndexerCollection(
            this._facade,
            this.Compilation.GetIndexerCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

    [Memo]
    public IIndexerCollection AllIndexers => new AllIndexersCollection( this._facade );

    [Memo]
    public IFieldCollection Fields
        => new FieldCollection(
            this._facade,
            this.Compilation.GetFieldCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

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
            this.Compilation.GetEventCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

    [Memo]
    public IEventCollection AllEvents => new AllEventsCollection( this._facade );

    [Memo]
    public IMethodCollection Methods
        => new MethodCollection(
            this._facade,
            this.Compilation.GetMethodCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

    [Memo]
    public IMethodCollection AllMethods => new AllMethodsCollection( this._facade );

    [Memo]
    public IConstructorCollection Constructors
        => new ConstructorCollection(
            this._facade,
            this.Compilation.GetConstructorCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) ) );

    [Memo]
    public IConstructor? PrimaryConstructor => this.GetPrimaryConstructorImpl();

    [Memo]
    public IConstructor? StaticConstructor => this.GetStaticConstructorImpl();

    public IMethod? Finalizer => this.GetFinalizerImpl();

    private IConstructor? GetPrimaryConstructorImpl()
    {
        var constructors = this.Compilation.GetConstructorCollection( this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ) );

        foreach ( var constructor in constructors )
        {
            if ( constructor is ISymbolRef { Symbol: IMethodSymbol methodSymbol } && methodSymbol.IsPrimaryConstructor() )
            {
                return this.Compilation.Factory.GetConstructor( methodSymbol );
            }

            // TODO: Builders? (In case we e.g. add a parameter)
        }

        return null;
    }

    private IConstructor? GetStaticConstructorImpl()
    {
        var builder = this.Compilation.GetStaticConstructor( this._namedTypeSymbol );

        if ( builder != null )
        {
            return this.Compilation.Factory.GetConstructor( builder );
        }

        var symbol = this._namedTypeSymbol.StaticConstructors.SingleOrDefault();

        if ( symbol != null )
        {
            return this.Compilation.Factory.GetConstructor( symbol );
        }

        return null;
    }

    private IMethod? GetFinalizerImpl()
    {
        var builder = this.Compilation.GetFinalizer( this._namedTypeSymbol );

        if ( builder != null )
        {
            return this.Compilation.Factory.GetMethod( builder );
        }

        var symbol = this._namedTypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .SingleOrDefault( m => m is { Name: "Finalize", TypeParameters.Length: 0, Parameters.Length: 0 } );

        if ( symbol != null )
        {
            return this.Compilation.Factory.GetMethod( symbol );
        }

        return null;
    }

    public override bool IsPartial
    {
        get
        {
            var syntaxReference = this._namedTypeSymbol.GetPrimarySyntaxReference();

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

            return modifiers.Any( SyntaxKind.PartialKeyword );
        }
    }

    [Memo]
    public ITypeParameterList TypeParameters
        => new TypeParameterList(
            this._facade.Definition,
            this._namedTypeSymbol.TypeParameters.Select( x => this.RefFactory.FromSymbol<ITypeParameter>( x ) )
                .ToReadOnlyList() );

    INamespace INamedType.Namespace => this.ContainingNamespace;

    [Memo]
    public INamespace ContainingNamespace
        =>

            // Empty error type symbols (like unspecified type parameter) have null namespace.
            // Other error types usually have assembly-specific global namespace.
            this._namedTypeSymbol.ContainingNamespace != null
                ? this.Compilation.Factory.GetNamespace( this._namedTypeSymbol.ContainingNamespace )
                : this.Compilation.GlobalNamespace;

    IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Ref;

    [Memo]
    public string FullName => this._namedTypeSymbol.GetFullName().AssertNotNull();

    [Memo]
    public IReadOnlyList<IType> TypeArguments
        => this.GenericContextForSymbolMapping.IsEmptyOrIdentity
            ? this._namedTypeSymbol.TypeArguments.SelectAsImmutableArray( a => this.Compilation.Factory.GetIType( a, this.GenericContextForSymbolMapping ) )
            : this.GenericContextForSymbolMapping.TypeArguments.SelectAsImmutableArray( t => t.GetTarget( this.Compilation ) );

    [Memo]
    public override IDeclaration ContainingDeclaration
        => this._namedTypeSymbol.ContainingSymbol switch
        {
            INamespaceSymbol => this.Compilation.Factory.GetAssembly( this._namedTypeSymbol.ContainingAssembly ),
            INamedTypeSymbol containingType => this.Compilation.Factory.GetNamedType( containingType, this.GenericContextForSymbolMapping ),
            null => this.Compilation, // Empty error type symbol goes here. Other error types return a namespace, which we handle above.
            _ => throw new AssertionFailedException( $"Unexpected containing symbol kind: {this._namedTypeSymbol.ContainingSymbol.Kind}." )
        };

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    [Memo]
    public INamedType? BaseType
        => this._namedTypeSymbol.BaseType == null
            ? null
            : this.Compilation.Factory.GetNamedType( this._namedTypeSymbol.BaseType, this.GenericContextForSymbolMapping );

    [Memo]
    public IImplementedInterfaceCollection AllImplementedInterfaces
        => new AllImplementedInterfacesCollection(
            this._facade,
            this.Compilation.GetAllInterfaceImplementationCollection(
                this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ),
                false ) );

    [Memo]
    public IImplementedInterfaceCollection ImplementedInterfaces
        => new ImplementedInterfacesCollection(
            this._facade,
            this.Compilation.GetInterfaceImplementationCollection(
                this._namedTypeSymbol.OriginalDefinition.ToRef( this.RefFactory ),
                false ) );

    ICompilation ICompilationElement.Compilation => this.Compilation;

    public INamedType MakeGenericInstance( IReadOnlyList<IType> typeArguments )
    {
        if ( typeArguments.Any( GenericContext.ReferencesAnyIntroducedType ) )
        {
            var genericContext = new IntroducedGenericContext(
                typeArguments.SelectAsImmutableArray( t => t.ToFullRef() ),
                this.ToFullDeclarationRef(),
                this.GenericContextForSymbolMapping as IntroducedGenericContext );

            return this.Compilation.Factory.GetNamedType( this._namedTypeSymbol.ConstructedFrom, genericContext );
        }
        else
        {
            var typeArgumentSymbols =
                typeArguments.SelectAsArray( a => a.GetSymbol().AssertSymbolNotNull() );

            var typeSymbol = this._namedTypeSymbol;
            var constructedTypeSymbol = typeSymbol.ConstructedFrom.Construct( typeArgumentSymbols );

            return this.Compilation.Factory.GetNamedType( constructedTypeSymbol );
        }
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
                var candidateMember = (IMember) this.Compilation.Factory.GetDeclaration( candidateSymbol, this.GenericContextForSymbolMapping );

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
        // TODO: Generic context

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
        // TODO: Generic context.

        // Fastest track: check if we are the required interface itself.
        if ( interfaceMember.DeclaringType.Equals( this ) )
        {
            implementationMember = interfaceMember;

            return true;
        }

        // Try to find using symbols.
        var symbolInterfaceMemberSymbol = interfaceMember.GetSymbol();

        if ( symbolInterfaceMemberSymbol != null )
        {
            var symbolInterfaceMemberImplementationSymbol =
                this._namedTypeSymbol.FindImplementationForInterfaceMember( symbolInterfaceMemberSymbol );

            if ( symbolInterfaceMemberImplementationSymbol != null )
            {
                implementationMember = (IMember) this.Compilation.Factory.GetDeclaration( symbolInterfaceMemberImplementationSymbol );

                return true;
            }
        }

        // Find the member in introduced interfaces, including in subtypes.
        var currentTypeDefinition = this.Definition;
        var currentTypeSymbol = this._namedTypeSymbol;

        while ( true )
        {
            var currentGenericContext = SymbolGenericContext.Get( currentTypeSymbol, this.GetCompilationContext() );

            var introducedInterface =
                this.Compilation
                    .GetInterfaceImplementationCollection( currentTypeDefinition.ToFullRef(), false )
                    .Introductions
                    .SingleOrDefault( i => currentGenericContext.Map( i.InterfaceType.GetTarget( this.Compilation ) ).Equals( interfaceMember.DeclaringType ) );

            if ( introducedInterface != null )
            {
                // TODO: Use the generic map to match (compare) the member in the following call to MemberMap.TryGetValue or FindMemberOfSignature.

                if ( introducedInterface.MemberMap.TryGetValue( interfaceMember, out var interfaceMemberImplementation ) )
                {
                    // We found it as an explicit member.
                    implementationMember = interfaceMemberImplementation.ForCompilation( this.Compilation );

                    return true;
                }
                else
                {
                    // Match in all members.
                    var candidate = FindMemberOfSignature( currentTypeDefinition, interfaceMember );

                    if ( candidate?.Accessibility == Accessibility.Public )
                    {
                        implementationMember = candidate.ForCompilation( this.Compilation );

                        return true;
                    }
                    else
                    {
                        implementationMember = null;

                        return false;
                    }
                }
            }

            if ( currentTypeSymbol.BaseType == null )
            {
                break;
            }
            else
            {
                currentTypeSymbol = currentTypeSymbol.BaseType;
                currentTypeDefinition = currentTypeDefinition.BaseType.AssertNotNull().Definition;
            }
        }

        implementationMember = null;

        return false;
    }

    [Memo]
    public INamedType Definition
        => this._namedTypeSymbol.Equals( this._namedTypeSymbol.OriginalDefinition )
            ? this
            : this.Compilation.Factory.GetNamedType( this._namedTypeSymbol.OriginalDefinition );

    INamedType INamedType.TypeDefinition => throw new NotSupportedException();

    protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => throw new NotSupportedException();

    [Memo]
    public INamedType UnderlyingType => this.GetUnderlyingTypeCore();

    private INamedType GetUnderlyingTypeCore()
    {
        var enumUnderlyingType = this._namedTypeSymbol.EnumUnderlyingType;

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
                return this.Compilation.Factory.GetNamedType( (INamedTypeSymbol) this._namedTypeSymbol.WithNullableAnnotation( NullableAnnotation.None ) );
            }
        }

        // Fall back to self.
        return this._facade;
    }

    private void PopulateAllInterfaces( ImmutableHashSet<INamedTypeSymbol>.Builder builder, in SymbolBasedGenericMap genericMap )
    {
        var compilation = this.Compilation.RoslynCompilation;

        // Process the Roslyn type system.
        foreach ( var type in this._namedTypeSymbol.AllInterfaces )
        {
            builder.Add( genericMap.SubstituteSymbol( type, compilation ) );
        }

        if ( this._namedTypeSymbol.BaseType != null )
        {
            var newGenericMap = genericMap.SubstituteSymbols( this._namedTypeSymbol.BaseType.TypeArguments, compilation );
            ((SourceNamedType) this.BaseType!).Implementation.PopulateAllInterfaces( builder, newGenericMap );
        }

        // TODO: process introductions.
    }

    private ImmutableHashSet<INamedTypeSymbol> GetAllInterfaces()
    {
        var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>( this.GetCompilationContext().SymbolComparer );
        this.PopulateAllInterfaces( builder, SymbolBasedGenericMap.Empty );

        return builder.ToImmutable();
    }

    IType ITypeImpl.Accept( TypeRewriter visitor ) => throw new NotSupportedException();

    public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

    public bool Equals( INamedType? other ) => this.Equals( other, TypeComparison.Default );

    public override int GetHashCode() => this.GetCompilationContext().SymbolComparer.GetHashCode( this._namedTypeSymbol );

    [Memo]
    public IFullRef<INamedType> Ref => this.RefFactory.FromSymbolBasedDeclaration<INamedType>( this );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<INamedType> INamedType.ToRef() => this.Ref;

    IRef<IType> IType.ToRef() => this.Ref;

    protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;
}