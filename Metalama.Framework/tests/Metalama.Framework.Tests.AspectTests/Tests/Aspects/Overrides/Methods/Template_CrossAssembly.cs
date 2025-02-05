namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.Template_CrossAssembly
{
    // <target>
    [TestAspect]
    internal class TargetClass
    {
        public void VoidMethod()
        {
        }

        public int Method(int x)
        {
            return x;
        }
    }
}
