using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.AccessorVisibility_Declarative
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int PropertyWithRestrictedGet
        {
            private get
            {
                return 42;
            }

            set { }
        }

        [Introduce]
        public int AutoPropertyWithRestrictedGet { private get; set; }

        [Introduce]
        public int PropertyWithRestrictedSet
        {
            get
            {
                return 42;
            }

            private set { }
        }

        [Introduce]
        public int AutoPropertyWithRestrictedSet { get; private set; }

        [Introduce]
        public int PropertyWithRestrictedInit
        {
            get
            {
                return 42;
            }

            private init { }
        }

        [Introduce]
        public int AutoPropertyWithRestrictedInit { get; private init; }

        [Introduce]
        protected int ProtectedAutoPropertyWithPrivateProtectedSetter { get; private protected set; }

        [Introduce]
        protected internal int ProtectedInternalAutoPropertyWithProtectedSetter { get; protected set; }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}