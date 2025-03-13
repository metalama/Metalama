// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.InternalPipeline.Templating.Syntax.Invocation.RunTimeTargets
{
    internal class Aspect : Attribute
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Method
            TargetCode.Method( 0, 1 );

            // Local variable.
            var local = new Func<int, int>( x => x );
            _ = local( 0 );

            // Field
            TargetCode.Field( 0, 1 );

            // Property
            TargetCode.Property( 0, 1 );

            // Expression
            _ = new Func<int, int>( x => x )( 0 );

            // Run-time dynamic field.
            TargetCode.DynamicField( 0, 1 );

            return null;
        }

        private static IExpression? BuildTimeMethod( int x, int y ) => null;
    }

    internal class TargetCode
    {
        public static dynamic DynamicField { get; }

        public static Action<int, int> Field;

        public static Action<int, int> Property { get; }

        [Aspect]
        public static void Method( int a, int b ) { }
    }
}