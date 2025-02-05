using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28880;

#pragma warning disable CS0169

// This checks that throw expressions in expression bodies work properly.

[assembly:
    AspectOrder( AspectOrderDirection.RunTime, typeof(TestMethodAspect), typeof(TestPropertyAspect), typeof(TestPropertyAspect2), typeof(TestEventAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28880
{
    internal class TestMethodAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod() => throw new NotSupportedException();
    }

    internal class TestPropertyAspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }

    internal class TestPropertyAspect2 : FieldOrPropertyAspect
    {
        [Template]
        public dynamic OverrideProperty => throw new NotSupportedException();

        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(OverrideProperty) );
        }
    }

    internal class TestEventAspect : OverrideEventAspect
    {
        public override void OverrideAdd( dynamic value ) => throw new NotImplementedException();

        public override void OverrideRemove( dynamic value ) => throw new NotImplementedException();
    }

    // <target>
    internal class TargetCode
    {
        [TestMethodAspect]
        private int Method( int a )
        {
            return a;
        }

        [TestPropertyAspect]
        private int field;

        [TestPropertyAspect]
        private int Property { get; set; }

        [TestPropertyAspect2]
        private int Property2 { get; set; }
    }
}