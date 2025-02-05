using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_ParamAndReturn
{
    internal class FilterAttribute : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.With( builder.Target.ReturnParameter ).AddContract( nameof(Filter) );

            foreach (var parameter in builder.Target.Parameters)
            {
                builder.With( parameter ).AddContract( nameof(Filter) );
            }
        }

        [Template]
        public void Filter( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    internal class Target
    {
        [Filter]
        private string? M( string? param1, int? param2 )
        {
            return param1 + param2.ToString();
        }
    }
}