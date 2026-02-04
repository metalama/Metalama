// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

internal sealed class CompilationHelpers : ICompilationHelpers
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly CompilationContext _compilationContext;
    private AttributeDeserializer? _attributeDeserializer;
    private bool? _hasAnyInternalsVisibleToAttribute;

    public CompilationHelpers( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
    {
        this._serviceProvider = serviceProvider;
        this._compilationContext = compilationContext;
    }

    // The service is not always available in tests, so we get it lazily.
    private AttributeDeserializer GetAttributeDeserializer()
        => this._attributeDeserializer ??=
            this._serviceProvider.GetRequiredService<UserCodeAttributeDeserializer.Provider>().Get( this._compilationContext );

    public IteratorInfo GetIteratorInfo( IMethod method ) => method.GetIteratorInfoImpl();

    public AsyncInfo GetAsyncInfo( IMethod method ) => method.GetAsyncInfoImpl();

    public AsyncInfo GetAsyncInfo( IType type ) => type.GetAsyncInfoImpl();

    public string GetMetadataName( INamedType type ) => type.GetReflectionName();

    public string GetFullMetadataName( INamedType type ) => type.GetReflectionFullName();

    public SerializableTypeId GetSerializableId( IType type ) => type.GetSerializableTypeId();

    public IExpression ToTypeOfExpression( IType type ) => new TypeOfUserExpression( type );

    public bool DerivesFrom( INamedType left, INamedType right, DerivedTypesOptions options = DerivedTypesOptions.Default )
    {
        if ( !ReferenceEquals( right.Definition, right ) )
        {
            throw new ArgumentOutOfRangeException( nameof(right), "The type must not be a generic type instance." );
        }

        // We do not include the right type itself.
        if ( left.Definition.Equals( right ) )
        {
            return false;
        }

        switch ( options )
        {
            case DerivedTypesOptions.All:
                return IsEqualOrDerivesFromWithAnyDegree( left );

            case DerivedTypesOptions.DirectOnly:
                return DerivesFromDirectly( left );

            case DerivedTypesOptions.FirstLevelWithinCompilationOnly:
                return DerivesFromWithFirstLevel( left );

            default:
                throw new ArgumentOutOfRangeException( nameof(options) );
        }

        bool IsEqualOrDerivesFromWithAnyDegree( INamedType type )
        {
            if ( type.Equals( right ) )
            {
                return true;
            }

            if ( type.BaseType != null )
            {
                if ( IsEqualOrDerivesFromWithAnyDegree( type.BaseType.Definition ) )
                {
                    return true;
                }
            }

            foreach ( var i in type.ImplementedInterfaces )
            {
                if ( IsEqualOrDerivesFromWithAnyDegree( i.Definition ) )
                {
                    return true;
                }
            }

            return false;
        }

        bool DerivesFromDirectly( INamedType type )
        {
            if ( type.BaseType != null )
            {
                var baseType = type.BaseType.Definition;

                if ( baseType.Equals( right ) )
                {
                    return true;
                }
            }

            foreach ( var i in type.ImplementedInterfaces )
            {
                if ( i.Definition.Equals( right ) )
                {
                    return true;
                }
            }

            return false;
        }

        bool DerivesFromWithFirstLevel( INamedType type )
        {
            if ( type.BaseType != null && !type.BaseType.DeclaringAssembly.Equals( type.DeclaringAssembly ) )
            {
                if ( IsEqualOrDerivesFromWithAnyDegree( type.BaseType.Definition ) )
                {
                    return true;
                }
            }

            foreach ( var i in type.ImplementedInterfaces )
            {
                if ( !i.DeclaringAssembly.Equals( type.DeclaringAssembly ) && IsEqualOrDerivesFromWithAnyDegree( i.Definition ) )
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool TryConstructAttribute(
        IAttribute attribute,
        ScopedDiagnosticSink diagnosticSink,
        [NotNullWhen( true )] out Attribute? constructedAttribute )
    {
        return this.GetAttributeDeserializer()
            .TryCreateAttribute(
                attribute,
                (IDiagnosticAdder?) diagnosticSink.Sink ?? NullDiagnosticAdder.Instance,
                out constructedAttribute );
    }

    public Attribute ConstructAttribute( IAttribute attribute )
    {
        if ( this.GetAttributeDeserializer().TryCreateAttribute( attribute, ThrowingDiagnosticAdder.Instance, out var constructedAttribute ) )
        {
            return constructedAttribute;
        }

        throw new AssertionFailedException( "The attribute construction failed, but no error message was reported." );
    }

    public bool IsAccessibleFrom( IMemberOrNamedType accessedMember, INamedType accessingType )
    {
        bool? hasInternalAccess = null;

        return IsAccessibleFromCore( accessedMember, accessingType, ref hasInternalAccess );
    }

    private static bool IsAccessibleFromCore( IMemberOrNamedType accessedMember, INamedType accessingType, ref bool? hasInternalAccess )
        => accessedMember.Accessibility switch
        {
            Accessibility.Public => accessedMember.DeclaringType == null
                                    || IsAccessibleFromCore( accessedMember.DeclaringType, accessingType, ref hasInternalAccess ),
            Accessibility.Protected => IsDerivedFromOrContainedIn( accessedMember.DeclaringType.AssertNotNull(), accessingType ),
            Accessibility.Private => accessingType.IsContainedIn( accessedMember.DeclaringType.AssertNotNull() ),
            Accessibility.Internal when accessedMember.DeclaringType == null => AreInternalsVisibleFrom(
                accessedMember.Compilation,
                accessingType.Compilation,
                ref hasInternalAccess ),
            Accessibility.Internal => IsAccessibleFromCore( accessedMember.DeclaringType, accessingType, ref hasInternalAccess ),
            Accessibility.PrivateProtected => IsDerivedFromOrContainedIn( accessedMember.DeclaringType.AssertNotNull(), accessingType )
                                              && IsAccessibleFromCore( accessedMember.DeclaringType, accessingType, ref hasInternalAccess ),
            Accessibility.ProtectedInternal =>
                IsDerivedFromOrContainedIn( accessedMember.DeclaringType.AssertNotNull(), accessingType )
                || IsAccessibleFromCore( accessedMember.DeclaringType, accessingType, ref hasInternalAccess ),
            _ => throw new AssertionFailedException( $"Unexpected accessibility: {accessedMember.Accessibility}." )
        };

    private static bool AreInternalsVisibleFrom( ICompilation accessed, ICompilation accessing, ref bool? cached )
        => cached ??= accessed.AreInternalsVisibleFrom( accessing );

    private static bool IsDerivedFromOrContainedIn( INamedType baseType, INamedType derivedType )
        => derivedType.IsConvertibleTo( baseType, ConversionKind.Reference ) || derivedType.IsContainedIn( baseType );

    public bool IsAccessibleFromOutsideAssembly( IDeclaration declaration, bool honorInternalVisibleToAttributes )
    {
        if ( declaration.GetCompilationContext() != this._compilationContext )
        {
            throw new ArgumentOutOfRangeException( nameof(declaration), "The declaration does not belong to the current compilation." );
        }

        return this.IsAccessibleFromOutsideAssemblyCore( declaration, honorInternalVisibleToAttributes );
    }

    private bool IsAccessibleFromOutsideAssemblyCore( IDeclaration declaration, bool honorInternalVisibleToAttributes )
    {
        if ( declaration.DeclarationKind.IsMemberOrNamedType
             && declaration is IMemberOrNamedType memberOrType )
        {
            switch ( memberOrType )
            {
                case { Accessibility: Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedInternal }
                    when memberOrType.DeclaringType != null:
                    return this.IsAccessibleFromOutsideAssemblyCore(
                        memberOrType.DeclaringType,
                        honorInternalVisibleToAttributes );

                case { Accessibility: Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedInternal }:
                    return true;

                case { Accessibility: Accessibility.Internal or Accessibility.PrivateProtected }
                    when this.HasAnyInternalsVisibleTo( honorInternalVisibleToAttributes ):
                    {
                        if ( memberOrType.DeclaringType != null )
                        {
                            return this.IsAccessibleFromOutsideAssemblyCore(
                                memberOrType.DeclaringType,
                                honorInternalVisibleToAttributes );
                        }
                        else
                        {
                            return this.HasAnyInternalsVisibleTo( honorInternalVisibleToAttributes );
                        }
                    }
            }
        }
        else if ( declaration.ContainingDeclaration != null )
        {
            return this.IsAccessibleFromOutsideAssemblyCore( declaration.ContainingDeclaration, honorInternalVisibleToAttributes );
        }

        return false;
    }

    private bool HasAnyInternalsVisibleTo( bool honorInternalVisibleToAttributes ) => !honorInternalVisibleToAttributes || this.HasAnyInternalsVisibleToCore();

    private bool HasAnyInternalsVisibleToCore()
        => this._hasAnyInternalsVisibleToAttribute ??= this._compilationContext.Compilation.Assembly.GetAttributes()
            .Any( a => a.AttributeClass is { Name: nameof(InternalsVisibleToAttribute) } );
}