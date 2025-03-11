// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Tagging.cs)
#endif

// ReSharper disable UnusedParameter.Global

// <target>

namespace Metalama.Extensions.Multicast.AspectTests.Filter_ByParameterKind
{
    [AddTag( "In", AttributeTargetElements = MulticastTargets.Parameter, AttributeTargetParameterAttributes = MulticastAttributes.InParameter )]
    [AddTag( "Out", AttributeTargetElements = MulticastTargets.Parameter, AttributeTargetParameterAttributes = MulticastAttributes.OutParameter )]
    [AddTag( "Ref", AttributeTargetElements = MulticastTargets.Parameter, AttributeTargetParameterAttributes = MulticastAttributes.RefParameter )]
    [AddTag( "Return", AttributeTargetElements = MulticastTargets.ReturnValue )]
    public abstract class C
    {
        public int Method( int normalParam, in int inParam, out int outParam, ref int refParam )
        {
            outParam = 5;

            return 5;
        }
    }
}