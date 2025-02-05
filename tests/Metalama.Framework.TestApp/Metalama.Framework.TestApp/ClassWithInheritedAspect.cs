using Metalama.Framework.TestApp.Aspects;

namespace Metalama.Framework.TestApp
{
    partial class ClassWithInheritedAspect : IInterface
    {
        public void ManualMethod()
        {
            this.IntroducedMethod();
        }
    }
}