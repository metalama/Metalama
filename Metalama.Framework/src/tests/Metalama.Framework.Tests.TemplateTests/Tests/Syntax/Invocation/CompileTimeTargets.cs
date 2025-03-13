// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.InternalPipeline.Templating.Syntax.Invocation.CompileTimeTargets
{
    internal class Aspect : Attribute
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Method
            CompileTimeClass.Method( 0, 1 );

            // Local variable.
            var local = meta.CompileTime( new Func<int, int>( x => x ) );
            _ = local( 0 );

            // Field
            CompileTimeClass.Field( 0, 1 );

            // Property
            CompileTimeClass.Property( 0, 1 );

            // Expression
            meta.CompileTime( new Func<int, int>( x => x ) )( 0 );

            return null;
        }

        private static IExpression? BuildTimeMethod( int x, int y ) => null;
    }

    [CompileTime]
    internal class CompileTimeClass
    {
        public static Action<int, int> Field = ( x, y ) => { };

        public static Action<int, int> Property { get; } = ( x, y ) => { };

        public static void Method( int a, int b ) { }
    }

    internal class TargetCode
    {
        [Aspect]
        public static void Method( int a, int b ) { }
    }
}