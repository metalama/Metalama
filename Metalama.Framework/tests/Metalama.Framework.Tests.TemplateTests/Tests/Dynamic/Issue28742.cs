using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169, CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Issue28742
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            foreach (var fieldOrProperty in meta.Target.Type.FieldsAndProperties)
            {
                if (!fieldOrProperty.IsImplicitlyDeclared && fieldOrProperty.IsAutoPropertyOrField.GetValueOrDefault())
                {
                    var value = fieldOrProperty.Value;
                    Console.WriteLine( $"{fieldOrProperty.Name}={value}" );
                }
            }

            return default;
        }
    }

    // Placeholder implementation of a cache because the hosted try.postsharp.net does not allow for MemoryCache.

    // <target>
    internal class TargetCode
    {
        private int a;

        public string B { get; set; }

        private static int c;

        private void Method() { }
    }
}