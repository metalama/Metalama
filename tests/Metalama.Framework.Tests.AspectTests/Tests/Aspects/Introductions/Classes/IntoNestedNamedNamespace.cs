using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Introductions.Types.IntoNestedNamedNamespace
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var nsBuilder = builder.With( builder.Target.Compilation ).WithNamespace( "A.B.C.D.E.F" );

            var type = nsBuilder.IntroduceClass( "SomeClassFor" + builder.Target.Name );
            var field = type.IntroduceField( "field", TypeFactory.GetType( typeof(int) ), buildField: f => f.IsStatic = true ).Declaration;
            type.IntroduceMethod( nameof(SomeMethod), args: new { field } );
        }

        [Template]
        private static void SomeMethod( IField field )
        {
            field.Value = 5;
        }
    }

    // <target>
    namespace TargetNamespace
    {
        [IntroductionAttribute]
        public class TargetType { }

        [IntroductionAttribute]
        public class TargetType2 { }
    }
}