using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_ParamAndReturn_CrossAssembly
{
    [Inheritable]
    internal class FilterAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }

    public interface IInterface
    {
        [return: Filter]
        string? M( [Filter] string? param1, [Filter]  int? param2 );
    }

}