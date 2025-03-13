// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System;

namespace Metalama.Framework.TestApp
{
    public class SuppressWarningAttribute : Attribute, IAspect<IDeclaration>
    {
        static SuppressionDefinition _mySuppression1 = new("CS1998");
        static SuppressionDefinition _mySuppression2 = new("IDE0051");

        public SuppressWarningAttribute()
        {

        }

        public void BuildAspect(IAspectBuilder<IDeclaration> aspectBuilder)
        {
            aspectBuilder.Diagnostics.Suppress(_mySuppression1);
            aspectBuilder.Diagnostics.Suppress(_mySuppression2);
        }
    }
}
