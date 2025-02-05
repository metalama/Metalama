using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Iterators.Introduce
{
    internal class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(ProgrammaticallyMethodAsync) );
        }

        [Introduce]
        public IEnumerable<int> DeclarativelyMethodAsync()
        {
            yield return 1;
        }

        [Template]
        public IEnumerable<int> ProgrammaticallyMethodAsync()
        {
            yield return 1;
        }
    }

    // <target>
    [Aspect]
    internal class TargetCode { }
}