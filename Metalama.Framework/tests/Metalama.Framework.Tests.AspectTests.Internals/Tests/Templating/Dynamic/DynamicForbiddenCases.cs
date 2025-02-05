using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#pragma warning disable CS0168 // Variable is declared but never used
#pragma warning disable CS0169 // Field is declared but never used
#pragma warning disable CS0183 // 'is' expression's given expression is always of the provided type
#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS1981 // Using 'is' to test compatibility with 'dynamic' is essentially identical to testing compatibility with 'Object'

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicForbiddenCases
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var cast = (dynamic)0;

            var asCast = 0 as dynamic;

            var isCheck = 0 is dynamic;

            dynamic[] arrayType;

            List<dynamic> genericType;

            (dynamic, int) unnamedTupleType;

            (dynamic d, int i) namedTupleType;

            ref dynamic refType = ref Unsafe.NullRef<dynamic>();

            var defaultExpression = default(dynamic);

            var lambdaParameter = (dynamic d) => 0;

            var lambdaReturnType = dynamic () => null!;

            var newArray = new dynamic[0];

            var linq = from dynamic x in meta.Target.Parameters
                       join dynamic y in meta.Target.Parameters on x.Name equals y.Name
                       select x;

            foreach (dynamic item in meta.Target.Parameters)
            {
            }

            for (dynamic i = meta.Target; ;)
            {
                break;
            }

            dynamic initialization = meta.Target;

            dynamic refToDynamic = meta.Target.Parameter.Value!;
            ref var r = ref refToDynamic;

            dynamic noInitialization;

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}