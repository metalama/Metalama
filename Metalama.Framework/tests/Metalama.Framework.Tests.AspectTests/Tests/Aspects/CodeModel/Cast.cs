using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.CodeModel.Cast
{
    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var castNull = meta.Cast( meta.Target.Method.ReturnType, null );
            var castParam = meta.Cast( meta.Target.Method.ReturnType, (object?)meta.Target.Parameters[0].Value );
            var castLiteral = meta.Cast( ( (IParameter)meta.Target.Method.Parameters[1] ).Type, 1 );

            return default;
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public TargetClass? TargetMethod_Void( object o, decimal d )
        {
            return null;
        }
    }
}