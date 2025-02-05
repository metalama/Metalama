using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.NameConflict_Introduced;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InnerOverrideAttribute), typeof(OuterOverrideAttribute), typeof(IntroductionAttribute) )]
#pragma warning disable CS0219

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.NameConflict_Introduced
{
    public class InnerOverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods.Where( m => !m.IsImplicitlyDeclared ))
            {
                builder.With( method ).Override( nameof(OverrideMethod) );
            }
        }

        [Template]
        public dynamic? OverrideMethod()
        {
            var i = 27;

            return meta.Proceed();
        }
    }

    public class OuterOverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods.Where( m => !m.IsImplicitlyDeclared ))
            {
                builder.With( method ).Override( nameof(OverrideMethod) );
            }
        }

        [Template]
        public dynamic? OverrideMethod()
        {
            var i = 42;

            return meta.Proceed();
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public dynamic? IntroducedMethod_ConflictBetweenOverrides()
        {
            return meta.Proceed();
        }

        [Introduce]
        public dynamic? IntroducedMethod_ConflictWithParameter( int i )
        {
            return meta.Proceed();
        }
    }

    // <target>
    [InnerOverride]
    [OuterOverride]
    [Introduction]
    internal class TargetClass { }
}