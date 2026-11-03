// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.WeaverAndUnusedContractAspect_CrossProject
{
    // This contract aspect is declared in a referenced project, like the contract aspects of Metalama.Patterns.Contracts.
    public class NotNullAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if ( value == null )
            {
                throw new ArgumentNullException( meta.Target.Parameter.Name );
            }
        }
    }
}
