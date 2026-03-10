// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachTupleDeconstructionCodeModel
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time list of tuples containing code model elements, similar to the original issue
            var pairs = meta.CompileTime(
                meta.Target.Parameters.SelectAsArray( p => (p.Name, p.Type.ToDisplayString()) ) );

            foreach ( var (name, typeName) in pairs )
            {
                Console.WriteLine( $"Parameter: {name}, Type: {typeName}" );
            }

            var result = meta.Proceed();

            return result;
        }
    }

    internal class TargetCode
    {
        private int Method( int a, string b )
        {
            return a;
        }
    }
}
