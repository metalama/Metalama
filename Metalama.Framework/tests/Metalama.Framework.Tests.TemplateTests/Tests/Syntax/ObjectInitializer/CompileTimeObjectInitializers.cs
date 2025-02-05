using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.CompileTimeObjectInitializers
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var a = new Entity1 { Property1 = 1, Property2 = { new Entity2 { Property1 = 2 }, new Entity2 { Property1 = 3 } } };

            var b = a with { Property1 = 2 };

            Console.WriteLine( a.Property1 );

            var result = meta.Proceed();

            return result;
        }
    }

    [CompileTime]
    internal record Entity1
    {
        public int Property1 { get; set; }

        public IList<Entity2> Property2 { get; set; } = new List<Entity2>();
    }

    [CompileTime]
    internal struct Entity2
    {
        public int Property1 { get; set; }
    }

    internal class TargetCode
    {
        private object Method( object a )
        {
            return a;
        }
    }
}