using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_ThisError
{
    public class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            Console.WriteLine( $"{meta.Target.Type.Name} {meta.This}: {meta.AspectInstance.AspectClass.ShortName}" );
        }
    }

    // <target>
    [Aspect]
    public class TargetCode
    {
        public TargetCode() { }

        static TargetCode() { }

        private int Method( int a )
        {
            return a;
        }
    }
}