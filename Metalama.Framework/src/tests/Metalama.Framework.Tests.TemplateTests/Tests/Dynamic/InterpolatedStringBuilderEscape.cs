// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.InterpolatedStringBuilderEscape
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Normal literals.
            Console.WriteLine( "\\\n{}\"" );
            Console.WriteLine( meta.CompileTime( "\\" + "\n{}\"" ) );

            // Interpolated string.
            var s = new InterpolatedStringBuilder();
            s.AddText( "{ " );
            s.AddText( "$" );
            s.AddText( "\\" );
            s.AddText( "\n" );
            s.AddText( " }" );

            var a = s.ToValue();

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