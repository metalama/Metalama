using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.PartialMethod_SyntaxTrees
{

    // <target>
    internal partial class TargetClass
    {
        public partial int TargetMethod()
        {
            Console.WriteLine("This is a partial method.");
            return 42;
        }

        partial void TargetVoidMethodWithImplementation()
        {
            Console.WriteLine("This is a partial method.");
        }
    }
}