#if TEST_OPTIONS
// @LanguageVersion(10)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.Target_Struct_InstanceConstructing_Implicit_CSharp10
{
    public class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName}" );
        }
    }

    // <target>
    [Aspect]
    public struct TargetStruct
    {
        private int Method( int a )
        {
            return a;
        }
    }
}