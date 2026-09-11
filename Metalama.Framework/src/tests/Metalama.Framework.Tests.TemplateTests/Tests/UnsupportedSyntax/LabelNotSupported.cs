// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// A label in a template is not supported, for the reason given in section 5 of
// Metalama.Framework/docs/2027.0/DECISIONS.md: the annotator cannot decide whether the label is run-time or
// compile-time, because the label belongs to a loop whose scope may differ from the scope of the statement that names
// it. See issue #1947.
//
// CS0164 is disabled because the label of this template is never referenced. A label may be referenced by goto, which
// the template annotator also rejects, or by a labeled break or continue, which the language version of this project
// does not accept.

#pragma warning disable CS0164
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System;

namespace Metalama.Framework.Tests.AspectTests.Templating.UnsupportedSyntax.LabelNotSupported
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
        outer:

            for ( var i = 0; i < 2; i++ )
            {
                Console.WriteLine( i );
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}
