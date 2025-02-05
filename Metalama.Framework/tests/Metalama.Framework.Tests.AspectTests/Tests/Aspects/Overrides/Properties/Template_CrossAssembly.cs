using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Template_CrossAssembly
{
    // <target>
    [TestAspect]
    internal class TargetClass
    {
        public string Property
        {
            get
            {
                Console.WriteLine("Aspect code");
                return "Test";
            }

            set
            {
                Console.WriteLine("Aspect code");
            }
        }

        public string ExpressionProperty => "Test";

        public string? AutoProperty { get; set; }

        public string AutoPropertyWithInitializer { get; set; } = "Test";
    }
}
