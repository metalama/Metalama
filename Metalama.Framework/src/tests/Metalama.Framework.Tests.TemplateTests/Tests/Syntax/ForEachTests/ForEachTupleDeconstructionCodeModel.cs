// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
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
            // Compile-time list of tuples built from code model elements
            var pairs = meta.CompileTime( GetPairs() );

            foreach ( var (name, typeName) in pairs )
            {
                Console.WriteLine( $"Parameter: {name}, Type: {typeName}" );
            }

            var result = meta.Proceed();

            return result;
        }

        [CompileTime]
        private static List<(string Name, string TypeName)> GetPairs()
        {
#pragma warning disable CS0618 // Use SelectAsList or SelectAsArray - using Linq Select intentionally
            return meta.Target.Parameters.Select( p => (p.Name, p.Type.ToDisplayString()) ).ToList();
#pragma warning restore CS0618
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
