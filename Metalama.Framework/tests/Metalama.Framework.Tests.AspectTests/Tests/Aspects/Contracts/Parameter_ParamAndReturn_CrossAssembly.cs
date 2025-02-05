using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_ParamAndReturn_CrossAssembly
{

    // <target>
    internal class Target : IInterface
    {
        public string? M( string? param1, int? param2 )
        {
            return param1 + param2.ToString();
        }
    }
}