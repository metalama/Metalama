using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug18911
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
        public string? Property { get; set; }
    }
}