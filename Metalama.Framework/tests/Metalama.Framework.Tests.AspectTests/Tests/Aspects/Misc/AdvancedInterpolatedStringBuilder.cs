// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.AdvancedInterpolatedStringBuilder
{
    internal class GenerateMethodAttribute : TypeAspect
    {
        [Introduce]
        public string GeneratedMethod()
        {
            // Build an interpolated string that contains all parameters.
            var stringBuilder = new InterpolatedStringBuilder();

            stringBuilder.AddExpression( 0 );
            stringBuilder.AddExpression( "1", -5 );
            stringBuilder.AddExpression( 2, format: "x" );
            stringBuilder.AddExpression( 3, 10, "g" );

            stringBuilder.AddText( " a" );
            stringBuilder.AddExpression( "b" );
            stringBuilder.AddText( "c" );
            stringBuilder.AddExpression( "d", 3 );
            stringBuilder.AddText( "e" );
            stringBuilder.AddExpression( "f", format: "s" );
            stringBuilder.AddExpression( "f", format: null );

            stringBuilder.AddText( "{" );
            stringBuilder.AddExpression( 0 );
            stringBuilder.AddText( "}" );

            stringBuilder.AddExpression( new Random().Next() == 0 ? 0 : 1 );

            stringBuilder.AddText( "\"\\\r\n\t" );

            return stringBuilder.ToValue();
        }
    }

    // <target>
    [GenerateMethod]
    internal class Program { }
}