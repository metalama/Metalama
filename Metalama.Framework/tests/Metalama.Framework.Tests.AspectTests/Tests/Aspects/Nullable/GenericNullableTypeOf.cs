using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.GenericNullableTypeOf
{
#pragma warning disable CS8600, CS0169

    internal class MyAspect : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.With( builder.Target.DeclaringType ).IntroduceMethod( nameof(Template), args: new { T = builder.Target.Type } );
        }

        [Template]
        public void Template<[CompileTime] T>()
        {
            IServiceProvider serviceProvider = null!;
            var x = (T)serviceProvider.GetService( typeof(T) );
        }
    }

    // <target>
    internal class C
    {
        [MyAspect]
        private string? _f;
    }
}