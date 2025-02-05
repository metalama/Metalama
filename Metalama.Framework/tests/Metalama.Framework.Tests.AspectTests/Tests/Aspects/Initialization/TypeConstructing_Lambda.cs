using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_Lambda
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
            var action = ExpressionFactory.Capture( new Func<object, string>( _ => { return "Hello, world."; } ) );
            Invoke( action.Value );
        }

        [Introduce]
        public static void Invoke( Func<object, string> action ) { }
    }

    // <target>
    [Aspect]
    public class TargetCode
    {
        static TargetCode() { }
    }
}