// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesMixedTypeParameters.TestAspect1), typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesMixedTypeParameters.TestAspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesMixedTypeParameters
{
    public class TestAspect1 : TypeAspect
    {
        // This aspect introduces/overrides the method, renaming the type parameters from A, B, C to X, Y, Z.
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void Foo<X, Y, Z>()
        {
            Console.WriteLine( typeof(X) );
            meta.Proceed();
        }
    }

    public class TestAspect2 : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            if ( builder.Target.IsConvertibleTo( typeof(Base) ) )
            {
                builder.With( builder.Target.Methods.OfName( "Foo" ).Single() ).Override(
                    nameof(FooTemplate),
                    new { T2 = typeof(int), T4 = typeof(string) } );
            }
        }

        // The template uses mixed compile-time and run-time type parameters, not packed adjacently.
        // Run-time: T1, T3, T5 should map by position to the target's X, Y, Z.
        // Compile-time: T2, T4 are provided via arguments.
        [Template]
        public void FooTemplate<T1, [CompileTime] T2, T3, [CompileTime] T4, T5>()
        {
            Console.WriteLine( typeof(T1) );
            Console.WriteLine( typeof(T3) );
            Console.WriteLine( typeof(T5) );
            meta.Proceed();
        }
    }

    internal class Base
    {
        public virtual void Foo<A, B, C>()
        {
        }
    }

    // <target>
    [TestAspect1]
    [TestAspect2]
    internal class Target : Base
    {
    }
}
