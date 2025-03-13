// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

#pragma warning disable CS0162 // Unreacheable code.

namespace CodeCoverage
{
    public class NonInlineablePropertyAspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic OverrideProperty
        {
            get
            {
                if ( true )
                {
                    return meta.Proceed();
                }
                else
                {
                    return meta.Proceed();
                }
            }

            set
            {
                if ( true )
                {
                    meta.Proceed();
                }
                else
                {
                    meta.Proceed();
                }
            }
        }
    }

}