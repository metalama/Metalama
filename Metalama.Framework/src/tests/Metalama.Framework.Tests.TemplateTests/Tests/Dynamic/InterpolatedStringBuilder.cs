// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.InterpolatedStringBuilderT
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var s = new InterpolatedStringBuilder();
            s.AddText( meta.Target.Method.Name + "(" );

            foreach (var p in meta.Target.Parameters)
            {
                if (p.Index > 0)
                {
                    s.AddText( ", " );
                }

                s.AddText( $"{p.Name}=" );
                s.AddExpression( p.Value );
            }

            s.AddText( ") {guid=" );
            s.AddExpression( Guid.Parse( "04cee639-acf2-46e3-be3e-916089c72a1e" ) );
            s.AddText( "}" );

            var is1 = s.ToValue();
            var is2 = s.ToExpression().Value;

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