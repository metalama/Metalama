// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities.Caching;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Metalama.Framework.Engine.Utilities;

[PublicAPI]
internal static class FormatterHelper
{
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