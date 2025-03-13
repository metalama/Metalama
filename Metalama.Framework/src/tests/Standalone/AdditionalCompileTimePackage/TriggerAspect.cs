// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionTriggerAspect
{
    public class TriggerAspect : ParameterAspect
    {

        public TriggerAspect()
        {
        }

        public override void BuildAspect(IAspectBuilder<IParameter> builder)
        {
            var parms = new object[] { AuthorizationLevel.Anonymous, new string[] { "get" } };

            var namedParms = new Dictionary<string, object?>()
            {
                { "Route", "AspectBasedFunction/HelloWorld" }
            }.ToList();

            builder.Advice.IntroduceAttribute(builder.Target, AttributeConstruction.Create(typeof(HttpTriggerAttribute), parms, namedParms));
        }

    }
}
