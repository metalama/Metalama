using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.SourceProperty
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var property in builder.Target.Properties.Where( p => !p.IsAbstract && p.Writeability == Writeability.All ))
            {
                builder.Advice.OverrideAccessors( property, null, nameof(OverridePropertySetter) );
            }
        }

        [Template]
        private dynamic OverridePropertySetter( dynamic value )
        {
            if (value != meta.Target.Property.Value)
            {
                meta.Proceed();
            }

            return value;
        }
    }

    // <target>
    [TestAspect]
    public class Target
    {
        public int _field;

        public int GetAutoProperty { get; }

        public int InitAutoProperty { get; init; }

        public int AutoProperty { get; set; }

        public int Property
        {
            get => _field;
            set => _field = value;
        }
    }
}