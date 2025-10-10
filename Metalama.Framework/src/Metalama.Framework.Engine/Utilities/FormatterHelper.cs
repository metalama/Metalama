// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Metalama.Framework.Engine.Utilities;

[PublicAPI]
internal static class FormatterHelper
{
    private const char _backingFieldPrefix = '<';
    private const string _backingFieldSuffix = ">k__BackingField";

    public static string ToDebugString( this ISymbol symbol )
        => symbol switch
        {
            IParameterSymbol parameter => parameter.ContainingSymbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat ) + "/"
                + parameter.Name,
            IFieldSymbol { IsImplicitlyDeclared: true } field when TryGetBackedPropertyName( field.Name, out var propertyName ) => field.ContainingType
                .ToDebugString() + "." + propertyName + ".field",
            _ => symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )
        };

    public static bool TryGetBackedPropertyName( string fieldName, [NotNullWhen( true )] out string? propertyName )
    {
        // Format backing fields as PropertyName.field. Different Roslyn versions use different renderings,
        // and it's easier for tests if we unify it.

        if ( fieldName[0] == _backingFieldPrefix && fieldName.EndsWith( _backingFieldSuffix, StringComparison.Ordinal ) )
        {
            propertyName = fieldName.Substring( 1, fieldName.Length - _backingFieldSuffix.Length - 1 );

            return true;
        }
        else
        {
            propertyName = null;

            return false;
        }
    }

    public static string Format(
        CodeDisplayFormat? format,
        CodeDisplayContext? context,
        [InterpolatedStringHandlerArgument( nameof(format), nameof(context) )]
        ref InterpolatedStringHandler handler )
        => handler.ToString();

    [InterpolatedStringHandler]
    [PublicAPI]
    public readonly ref struct InterpolatedStringHandler
    {
        private readonly CodeDisplayFormat? _format;
        private readonly CodeDisplayContext? _context;
        private readonly ObjectPoolHandle<StringBuilder> _stringBuilder;

        public InterpolatedStringHandler( int literalLength, int formattedCount, CodeDisplayFormat? format, CodeDisplayContext? context )
        {
            this._format = format;
            this._context = context;
            this._stringBuilder = StringBuilderPool.Default.Allocate();
        }

        public void AppendLiteral( string s ) => this._stringBuilder.Value.Append( s );

        public void AppendFormatted( IDisplayable displayable )
            => this._stringBuilder.Value.Append( displayable.ToDisplayString( this._format, this._context ) );

        public void AppendFormatted( IEnumerable<IDisplayable> collection )
        {
            var first = true;

            foreach ( var item in collection )
            {
                if ( !first )
                {
                    this._stringBuilder.Value.Append( ", " );
                }

                first = false;

                this._stringBuilder.Value.Append( item.ToDisplayString( this._format, this._context ) );
            }
        }

        public void AppendFormatted( string s ) => this._stringBuilder.Value.Append( s );

        public override string ToString()
        {
            var s = this._stringBuilder.Value.ToString();
            this._stringBuilder.Dispose();

            return s;
        }
    }
}