using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_TwoAspects_NoConstructor;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(SecondAspect), typeof(FirstAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_TwoAspects_NoConstructor
{
    public class FirstAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName} First" );
        }
    }

    public class SecondAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName} Second" );
        }
    }

    // <target>
    [FirstAspect]
    [SecondAspect]
    public class TargetCode { }
}