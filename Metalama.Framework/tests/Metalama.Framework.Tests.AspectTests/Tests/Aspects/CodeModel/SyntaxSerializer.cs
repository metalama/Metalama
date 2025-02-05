using System.Linq;
using System.Reflection;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer
{
    // Tests override method attribute where target method body contains return from the middle of the method. which forces aspect linker to use jumps to inline the override.
    // Template stores the result into a variable.

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var methodInfo = meta.RunTime( meta.Target.Method.ToMethodInfo() );
            var methodBase = meta.RunTime( meta.Target.Method.ToMethodBase() );
            var memberInfo = meta.RunTime( meta.Target.Method.ToMemberInfo() );
            var parameterInfo = meta.RunTime( meta.Target.Method.Parameters[0].ToParameterInfo() );
            var returnValueInfo = meta.RunTime( meta.Target.Method.ReturnParameter.ToParameterInfo() );
            var type = meta.RunTime( meta.Target.Method.DeclaringType!.ToType() );
            var field = meta.Target.Method.DeclaringType.Fields.Single( f => !f.IsImplicitlyDeclared ).ToFieldOrPropertyInfo();
            var propertyAsFieldOrProperty = meta.Target.Method.DeclaringType.Properties.Single().ToFieldOrPropertyInfo();
            var property = meta.RunTime( meta.Target.Method.DeclaringType.Properties.Single().ToPropertyInfo() );
            var constructor = meta.RunTime( meta.Target.Method.DeclaringType.Constructors.Single().ToConstructorInfo() );
            var constructorParameter = meta.RunTime( meta.Target.Method.DeclaringType.Constructors.Single().Parameters.Single().ToParameterInfo() );
            var array = meta.RunTime( new MemberInfo[] { meta.Target.Method.ToMethodInfo(), meta.Target.Type.ToType() } );

            return default;
        }
    }

    // <target>
    internal class TargetClass
    {
        public TargetClass( int x ) { }

        public int Field;

        public int Property { get; set; }

        [Override]
        public void TargetMethod_Void( int x ) { }

        public int TargetMethod_Int( int x )
        {
            return 0;
        }
    }
}