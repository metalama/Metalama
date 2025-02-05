using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0626

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.Abstract
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty( nameof(AbstractProperty) );
        }

        [Template]
        public extern int AbstractProperty { get; set; }
    }

    // <target>
    [Introduction]
    internal abstract class TargetClass { }
}