// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.InterpolatedStringBuilder_
{
    internal class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            // Build an interpolated string that contains all parameters.
            var stringBuilder = new InterpolatedStringBuilder();
            stringBuilder.AddExpression( meta.Target.Method.Name );
            stringBuilder.AddText( "( " );

            foreach (var parameter in meta.Target.Parameters)
            {
                if (parameter.Index > 0)
                {
                    stringBuilder.AddText( ", " );
                }

                stringBuilder.AddExpression( parameter.Name );
                stringBuilder.AddText( " = " );

                if (parameter.RefKind != RefKind.Out)
                {
                    stringBuilder.AddExpression( parameter.Value );
                }
                else
                {
                    stringBuilder.AddText( "<out>" );
                }
            }

            stringBuilder.AddText( " )" );

            // Run-time code template.
            Console.WriteLine( "Entering " + stringBuilder.ToValue() );

            try
            {
                return meta.Proceed();
            }
            finally
            {
                Console.WriteLine( "Leaving " + stringBuilder.ToValue() );
            }
        }
    }

// <target>
    internal class Program
    {
        [Log]
        private static void MyMethod( string who )
        {
            // Some very typical business code.
            Console.WriteLine( $"Hello, {who}!" );
        }

        private static void TestMain()
        {
            MyMethod( "Lama" );
        }
    }
}