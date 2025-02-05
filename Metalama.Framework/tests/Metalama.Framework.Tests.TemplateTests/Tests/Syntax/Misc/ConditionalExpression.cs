using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Misc.ConditionalExpression
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            var extraField = meta.Target.Method.DeclaringType.Fields.OfName( "extra" ).SingleOrDefault();

            // compile-time condition, compile-time result
            Console.WriteLine( extraField != null ? extraField.Name : "undefined" );

            // run-time condition, compile-time result
            Console.WriteLine( meta.This.extra != null ? extraField!.Name : "undefined" );

            // run-time condition, run-time result, inside compile-time condition, run-time result
            var extra = extraField != null ? meta.This.extra != null ? meta.This.extra.Value : 0 : 0;

            return meta.Proceed() + extra;
        }
    }

    internal class TargetCode
    {
        private int? extra;

        private int Method( int a )
        {
            return a;
        }
    }
}