#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

#pragma warning disable CS0169

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Overrides.Fields.DesignTime
{
    internal class Aspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => meta.Proceed() + 1;
            set => throw new NotImplementedException();
        }
    }

    internal class TargetCode
    {
        [Aspect]
        private int field;
    }
}