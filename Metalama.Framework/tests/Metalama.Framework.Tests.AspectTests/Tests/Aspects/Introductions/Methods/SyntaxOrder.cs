using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.SyntaxOrder;

[assembly:
    AspectOrder(
        AspectOrderDirection.RunTime,
        typeof(Override4Attribute),
        typeof(Override3Attribute),
        typeof(Override2Attribute),
        typeof(Introduction2Attribute),
        typeof(Override1Attribute),
        typeof(Introduction1Attribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.SyntaxOrder
{
    public class Introduction1Attribute : TypeAspect
    {
        [Introduce]
        public void Foo()
        {
            Console.WriteLine( "This is introduced method." );
            meta.Proceed();
            meta.Proceed();
        }
    }

    public class Override1Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Methods.OfName( "Foo" ).Single() ).Override( nameof(Template) );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( "This is overridden (1) method." );
            meta.Proceed();
            meta.Proceed();
        }
    }

    public class Introduction2Attribute : TypeAspect
    {
        [Introduce]
        public void Bar()
        {
            Console.WriteLine( "This is introduced method." );
            meta.Proceed();
            meta.Proceed();
        }
    }

    public class Override2Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Methods.OfName( "Foo" ).Single() ).Override( nameof(Template) );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( "This is overridden (2) method." );
            meta.Proceed();
            meta.Proceed();
        }
    }

    public class Override3Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Methods.OfName( "Bar" ).Single() ).Override( nameof(Template) );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( "This is overridden (3) method." );
            meta.Proceed();
            meta.Proceed();
        }
    }

    public class Override4Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Methods.OfName( "Foo" ).Single() ).Override( nameof(Template) );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( "This is overridden (4) method." );
            meta.Proceed();
            meta.Proceed();
        }
    }

    // <target>
    [Introduction1]
    [Introduction2]
    [Override1]
    [Override2]
    [Override3]
    [Override4]
    internal class TargetClass { }
}