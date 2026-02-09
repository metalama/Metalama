// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesTypeParameter.TestAspect1), typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesTypeParameter.TestAspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesTypeParameter
{
    public class TestAspect1 : TypeAspect
    {
        // This aspect introduces/overrides the method changing the type parameter name from S to U.
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void Foo<U>()
        {
            Console.WriteLine( typeof(U) );
            meta.Proceed();
        }
    }

    public class TestAspect2 : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            if ( builder.Target.IsConvertibleTo( typeof(Base) ) )
            {
                builder.With( builder.Target.Methods.OfName( "Foo" ).Single() ).Override( nameof(FooTemplate) );
            }
        }

        // The template uses a different type parameter name T (neither the original S nor the renamed U).
        [Template]
        public void FooTemplate<T>()
        {
            Console.WriteLine( typeof(T) );
            meta.Proceed();
        }
    }

    internal class Base
    {
        public virtual void Foo<S>()
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
