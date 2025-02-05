using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Async.Introduce
{
    internal class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(ProgrammaticallyMethodAsync) );
        }

        [Introduce]
        public async Task DeclarativelyMethodAsync()
        {
            await Task.Yield();
        }

        [Template]
        public async Task ProgrammaticallyMethodAsync()
        {
            await Task.Yield();
        }
    }

    // <target>
    [Aspect]
    internal class TargetCode { }
}