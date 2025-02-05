using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28910
{
    public class EmptyOverrideFieldOrPropertyAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => meta.Proceed();
            set => meta.Proceed();
        }
    }

    // <target>
    internal class EmptyOverrideFieldOrPropertyExample
    {
        [EmptyOverrideFieldOrProperty]
        private int _field;
    }
}