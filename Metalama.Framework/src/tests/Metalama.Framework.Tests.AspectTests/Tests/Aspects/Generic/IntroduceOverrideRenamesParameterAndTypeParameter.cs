// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesParameterAndTypeParameter.TestAspect1), typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesParameterAndTypeParameter.TestAspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.IntroduceOverrideRenamesParameterAndTypeParameter
{
    public class TestAspect1 : TypeAspect
    {
        // This aspect introduces/overrides the method, renaming both the type parameter (T -> U)
        // and the regular parameter (value -> item).
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public U Foo<U>( U item )
        {
            return meta.Proceed()!;
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

        // The template uses the original names (T, value) as defined in the base class.
        [Template]
        public T FooTemplate<T>( T value )
        {
            return meta.Proceed()!;
        }
    }

    internal class Base
    {
        public virtual T Foo<T>( T value )
        {
            return value;
        }
    }

    // <target>
    [TestAspect1]
    [TestAspect2]
    internal class Target : Base
    {
    }
}
