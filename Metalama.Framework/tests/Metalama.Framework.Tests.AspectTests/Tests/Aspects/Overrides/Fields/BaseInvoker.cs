using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Overrides.Fields.BaseInvoker
{
    internal class Aspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => meta.Target.FieldOrProperty.Value;
            set => meta.Target.FieldOrProperty.Value = value;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private int field;
    }
}