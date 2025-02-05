using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.PartialType_SyntaxTrees
{

    // <target>
    internal partial class TargetClass
    {
        public void TargetMethod2()
        {
            Console.WriteLine("This is TargetMethod2.");
        }
    }
}