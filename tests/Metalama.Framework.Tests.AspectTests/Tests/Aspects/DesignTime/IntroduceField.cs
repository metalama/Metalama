#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.IntroduceField;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceField("ValueType", typeof(int), buildField: fieldBuilder => fieldBuilder.InitializerExpression = TypedConstant.Create(42));
        builder.IntroduceField("NullableValueType", typeof(int?), buildField: fieldBuilder => fieldBuilder.InitializerExpression = TypedConstant.Create(13));
        builder.IntroduceField("ReferenceType", typeof(string), buildField: fieldBuilder => fieldBuilder.InitializerExpression = TypedConstant.Create("foo"));
        builder.IntroduceField("NullableReferenceType", TypeFactory.GetType(typeof(string)).ToNullable(), buildField: fieldBuilder => fieldBuilder.InitializerExpression = TypedConstant.Create("bar"));
    }
}

// <target>
[Introduction]
internal partial class TargetClass { }