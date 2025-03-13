// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicExpressionBuilder
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var guid = meta.CompileTime( Guid.Parse( "04cee639-acf2-46e3-be3e-916089c72a1e" ) );

            var expressionBuilder = new ExpressionBuilder();
            expressionBuilder.AppendVerbatim( "Test( " );
            expressionBuilder.AppendLiteral( 1, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( 1D, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( 1F, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( "s\"\n" );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( 1M, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( 1L, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( 1UL, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( (byte)1, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( (sbyte)1, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( (short)1, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendLiteral( (ushort)1, true );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendExpression( 42 );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendExpression( guid );
            expressionBuilder.AppendVerbatim( ", " );

            var arrayBuilder = new ArrayBuilder();
            arrayBuilder.Add( 1 );
            arrayBuilder.Add( 2 );
            arrayBuilder.Add( 3 );
            arrayBuilder.Add( guid );

            expressionBuilder.AppendExpression( arrayBuilder );
            expressionBuilder.AppendVerbatim( ", " );
            expressionBuilder.AppendExpression( meta.Target.Parameters[0] );
            expressionBuilder.AppendVerbatim( ", typeof(" );
            expressionBuilder.AppendTypeName( ( (IExpression)meta.Target.Parameters[0] ).Type );
            expressionBuilder.AppendVerbatim( ") )" );

            var expression = expressionBuilder.ToExpression();
            Console.WriteLine( $"type={expression.Type}" );
            var value = expressionBuilder.ToValue();

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a, string c, DateTime dt )
        {
            return a;
        }
    }
}