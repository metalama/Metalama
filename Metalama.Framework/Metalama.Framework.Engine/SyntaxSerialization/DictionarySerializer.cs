// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class DictionarySerializer : ObjectSerializer
{
    public DictionarySerializer( SyntaxSerializationService serializers ) : base( serializers ) { }

    public override ExpressionSyntax Serialize( object dictionary, SyntaxSerializationContext serializationContext )
    {
        var dictionaryType = dictionary.GetType();
        var keyType = dictionaryType.GetGenericArguments()[0];
        var valueType = dictionaryType.GetGenericArguments()[1];

        var creationExpression = ObjectCreationExpression( serializationContext.GetTypeSyntax( dictionaryType ) );

        var defaultComparer = typeof(EqualityComparer<>)
            .MakeGenericType( keyType )
            .GetProperty( nameof(EqualityComparer<int>.Default), BindingFlags.Public | BindingFlags.Static )
            .AssertNotNull()
            .GetValue( null );

        var actualComparer = typeof(Dictionary<,>)
            .MakeGenericType( keyType, valueType )
            .GetProperty( nameof(Dictionary<int, int>.Comparer), BindingFlags.Public | BindingFlags.Instance )
            .AssertNotNull()
            .GetValue( dictionary )
            .AssertNotNull();

        if ( keyType == typeof(string) )
        {
            string? comparerName = null;

            if ( actualComparer is StringComparer sc )
            {
                if ( sc == StringComparer.Ordinal )
                {
                    comparerName = "Ordinal";
                }
                else if ( sc == StringComparer.OrdinalIgnoreCase )
                {
                    comparerName = "OrdinalIgnoreCase";
                }
                else if ( sc == StringComparer.InvariantCulture )
                {
                    comparerName = "InvariantCulture";
                }
                else if ( sc == StringComparer.InvariantCultureIgnoreCase )
                {
                    comparerName = "InvariantCultureIgnoreCase";
                }
                else if ( sc.Equals( StringComparer.CurrentCulture ) )
                {
                    comparerName = "CurrentCulture";
                }
                else if ( sc.Equals( StringComparer.CurrentCultureIgnoreCase ) )
                {
                    comparerName = "CurrentCultureIgnoreCase";
                }
                else
                {
                    // Unknown string comparer
                    throw SerializationDiagnosticDescriptors.UnsupportedDictionaryComparer.CreateException( sc.GetType() );
                }
            }
            else if ( actualComparer.Equals( EqualityComparer<string>.Default ) )
            {
                // It's the default string comparer.
            }
            else
            {
                // Unknown custom comparer for a string-keyed dictionary
                throw SerializationDiagnosticDescriptors.UnsupportedDictionaryComparer.CreateException( actualComparer.GetType() );
            }

            if ( comparerName != null )
            {
                var comparerExpression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    serializationContext.GetTypeSyntax( typeof(StringComparer) ),
                    IdentifierName( comparerName ) );

                creationExpression = creationExpression.AddArgumentListArguments( Argument( comparerExpression ) );
            }
        }
        else if ( actualComparer != defaultComparer )
        {
            // Unknown custom comparer
            throw SerializationDiagnosticDescriptors.UnsupportedDictionaryComparer.CreateException( actualComparer.GetType() );
        }

        var lt = new List<InitializerExpressionSyntax>();
        var nonGenericDictionary = (IDictionary) dictionary;

        foreach ( var key in nonGenericDictionary.Keys )
        {
            var value = nonGenericDictionary[key];

            lt.Add(
                InitializerExpression(
                    SyntaxKind.ComplexElementInitializerExpression,
                    SeparatedList<ExpressionSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            this.Service.Serialize( key, serializationContext ),
                            Token( SyntaxKind.CommaToken ),
                            this.Service.Serialize( value, serializationContext )
                        } ) ) );
        }

        creationExpression = creationExpression.WithInitializer(
            InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SeparatedList<ExpressionSyntax>( lt ) ) );

        return creationExpression;
    }

    public override Type InputType => typeof(IReadOnlyDictionary<,>);

    public override Type OutputType => typeof(Dictionary<,>);

    protected override ImmutableArray<Type> AdditionalSupportedTypes => ImmutableArray.Create( typeof(IDictionary<,>) );
}