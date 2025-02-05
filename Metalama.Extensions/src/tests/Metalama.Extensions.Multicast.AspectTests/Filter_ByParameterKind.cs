// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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