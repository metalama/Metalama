// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

internal static class CodeModelInternalExtensions
{
    public static CompilationModel GetCompilationModel( this ICompilationElement declaration ) => (CompilationModel) declaration.Compilation;

    public static RefFactory GetRefFactory( this ICompilationElement declaration ) => ((CompilationModel) declaration.Compilation).RefFactory;

    public static CompilationContext GetCompilationContext( this ICompilationElementImpl declaration ) => declaration.Compilation.CompilationContext;

    public static CompilationContext GetCompilationContext( this ICompilationElement declaration ) => declaration.GetCompilationModel().CompilationContext;

    public static AttributeData GetAttributeData( this IAttribute attribute )
    {
        if ( attribute is SourceAttribute attributeModel )
        {
            return attributeModel.AttributeData;
        }

        throw new ArgumentOutOfRangeException( nameof(attribute), "This is not a source attribute." );
    }

    public static bool IsAccessor( this IMethod method )
        => method.MethodKind switch
        {
            MethodKind.PropertyGet => true,
            MethodKind.PropertySet => true,
            MethodKind.EventAdd => true,
            MethodKind.EventRemove => true,
            MethodKind.EventRaise => true,
            _ => false
        };

    public static InsertPosition ToInsertPosition( this IRef declaration )
        => declaration switch
        {
            ISymbolRef symbolRef => symbolRef.Symbol.ToInsertPosition(),
            IIntroducedRef builtDeclarationRef => builtDeclarationRef.BuilderData.InsertPosition,
            _ => throw new AssertionFailedException()
        };

    public static InsertPosition ToInsertPosition( this ISymbol symbol )
    {
        var primaryDeclaration = symbol.GetPrimaryDeclarationSyntax();

        if ( primaryDeclaration != null )
        {
            var memberDeclaration = primaryDeclaration.FindMemberDeclaration();

            if ( memberDeclaration is BaseTypeDeclarationSyntax )
            {
                return new InsertPosition( InsertPositionRelation.Within, memberDeclaration );
            }
            else
            {
                return new InsertPosition( InsertPositionRelation.After, memberDeclaration );
            }
        }
        else
        {
            var primaryTypeDeclaration = symbol.ContainingType.GetPrimaryDeclarationSyntax().AssertNotNull();

            return new InsertPosition( InsertPositionRelation.Within, primaryTypeDeclaration.FindMemberDeclaration() );
        }
    }

    [PublicAPI]
    public static InsertPosition ToInsertPosition( this IDeclaration declaration )
    {
        using ( StackOverflowHelper.Detect() )
        {
            switch ( declaration )
            {
                case IntroducedDeclaration builtDeclaration:
                    return builtDeclaration.BuilderData.InsertPosition;

                case SymbolBasedDeclaration baseDeclaration:
                    return baseDeclaration.Symbol.ToInsertPosition();

                default:
                    throw new AssertionFailedException( $"Unexpected declaration: '{declaration}'." );
            }
        }
    }

    internal static SyntaxToken GetCleanName( this IMember member )
        => SyntaxFactory.Identifier(
            member.IsExplicitInterfaceImplementation
                ? member.Name.Split( '.' ).Last()
                : member.Name );
}