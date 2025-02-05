using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.WeaklyTypedSerialize
{
    internal class IgnoreValuesAttribute : OverrideFieldOrPropertyAspect
    {
        private readonly object[] _ignoredValues;

        public IgnoreValuesAttribute( params object[] values )
        {
            _ignoredValues = values;
        }

        public override dynamic? OverrideProperty
        {
            get => meta.Proceed();
            set
            {
                foreach (var ignoredValue in _ignoredValues)
                {
                    if (value == meta.Cast( meta.Target.FieldOrProperty.Type, meta.RunTime( ignoredValue ) ))
                    {
                        return;
                    }
                }

                meta.Proceed();
            }
        }
    }

    internal enum MyEnum
    {
        None,
        Something
    }

    // <target>
    internal class TargetCode
    {
        [IgnoreValuesAttribute( 0 )]
        public int F;

        [IgnoreValuesAttribute( "" )]
        public string? S;

        [IgnoreValues( MyEnum.None )]
        public MyEnum E;

        [IgnoreValues( MyEnum.Something )]
        public MyEnum E2;
    }
}