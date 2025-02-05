using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.Target_ReadOnlyStruct_ExistingInitializedField
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedField;

        [Introduce]
        public int IntroducedField_Initializer = 42;

        [Introduce]
        public static int IntroducedField_Static;

        [Introduce]
        public static int IntroducedField_Static_Initializer = 42;
    }

    // <target>
    [Introduction]
    internal readonly struct TargetStruct
    {
        public readonly int _fieldInitializedByCtor;
        public readonly int _fieldInitializedByExpression = 42;

        public TargetStruct()
        {
            _fieldInitializedByCtor = 42;
        }
    }
}