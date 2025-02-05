using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.NeutralObjectInitializers_RunTime
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var runTime1 = new Entity1
            {
                Property1 = meta.Target.Method.Parameters.Count, Property2 = { new Entity2 { Property1 = meta.This.Foo }, new Entity2 { Property1 = 3 } }
            };

            var result = meta.Proceed();

            return result;
        }
    }

    [RunTimeOrCompileTime]
    internal record Entity1
    {
        public int Property1 { get; set; }

        public IList<Entity2> Property2 { get; set; } = new List<Entity2>();
    }

    [RunTimeOrCompileTime]
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