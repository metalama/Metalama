// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    internal interface IAspectRepository
    {
        bool HasAspect( IDeclaration declaration, Type aspectType );

        IEnumerable<IAspectInstance> GetAspectInstances( IDeclaration declaration );
    }
}