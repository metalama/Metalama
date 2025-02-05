using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.AsyncMethod_CrossAssembly
{
    // <target>
    class TargetCode
    {
        [Aspect]
        int NormalMethod(int a)
        {
            return a;
        }
        
        [Aspect]
        async Task<int> AsyncTaskResultMethod(int a)
        {
            await Task.Yield();
            return a;
        }

        [Aspect]
        async Task AsyncTaskMethod()
        {
            await Task.Yield();
        }
    }
}