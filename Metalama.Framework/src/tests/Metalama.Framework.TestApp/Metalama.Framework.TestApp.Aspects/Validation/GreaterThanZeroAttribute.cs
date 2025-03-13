// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.TestApp.Aspects.Validation
{
    [CompileTime]
    public class GreaterThanZeroAttribute : ValidateAttribute
    {
        public override void Validate(string name, dynamic value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, "The value must be strictly greater than zero.");
            }
        }
    }
}
