using Metalama.Framework.Aspects;
using XenoAtom.Logging;

namespace Issue1531;

internal class LogAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Program.Logger.Info("Entering method: " + meta.Target.Method.Name);
        return meta.Proceed();
    }
}
