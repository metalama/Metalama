using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.InstanceConstructing_UseExistingInitializer
{
    public class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var initializer = builder.Target.Fields.OfName( "Field1" ).Single().InitializerExpression;
            var targetField = builder.Target.Fields.OfName( "Field2" ).Single();
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor, args: new { field = targetField, initializer = initializer } );
        }

        [Template]
        public void Template( [CompileTime] IField field, [CompileTime] IExpression initializer )
        {
            field.Value = initializer;
        }
    }

    // <target>
    [Aspect]
    public class TargetCode
    {
        public int Field1 = 42;
        public int Field2;

        public TargetCode() { }
    }
}