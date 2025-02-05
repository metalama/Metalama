using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_SharedBase;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_SharedBase
{
    public class AspectBase : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName}" );
        }
    }

    public class Aspect1 : AspectBase { }

    public class Aspect2 : AspectBase { }

    // <target>
    [Aspect1]
    [Aspect2]
    public class TargetCode
    {
        public TargetCode() { }
    }
}