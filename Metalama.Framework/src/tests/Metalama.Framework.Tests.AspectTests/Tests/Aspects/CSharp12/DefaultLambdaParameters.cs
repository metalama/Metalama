// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.DefaultLambdaParameters;

public class TheAspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        var addWithDefault = ( int addTo = 2 ) => addTo + 1;
        addWithDefault();
        addWithDefault( 5 );

        var counter = ( params int[] xs ) => xs.Length;
        counter();
        counter( 1, 2, 3 );

        var addWithDefault2 = AddWithDefaultMethod;
        addWithDefault2();
        addWithDefault2( 5 );

        var counter2 = CountMethod;
        counter2();
        counter2( 1, 2 );

        int AddWithDefaultMethod( int addTo = 2 )
        {
            return addTo + 1;
        }

        int CountMethod( params int[] xs )
        {
            return xs.Length;
        }
    }

    public override dynamic? OverrideMethod()
    {
        var addWithDefault = ( int addTo = 2 ) => addTo + 1;
        addWithDefault();
        addWithDefault( 5 );

        var counter = ( params int[] xs ) => xs.Length;
        counter();
        counter( 1, 2, 3 );

        var addWithDefault2 = AddWithDefaultMethod;
        addWithDefault2();
        addWithDefault2( 5 );

        var counter2 = CountMethod;
        counter2();
        counter2( 1, 2 );

        var addWithDefault3 = meta.CompileTime( ( int addTo = 2 ) => addTo + 1 );
        addWithDefault3();
        addWithDefault3( 5 );

        var counter3 = meta.CompileTime( ( params int[] xs ) => xs.Length );
        counter3();
        counter3( 1, 2, 3 );

        int AddWithDefaultMethod( int addTo = 2 )
        {
            return addTo + 1;
        }

        int CountMethod( params int[] xs )
        {
            return xs.Length;
        }

        return meta.Proceed();
    }
}

public class C
{
    [TheAspect]
    private void M()
    {
        var addWithDefault = ( int addTo = 2 ) => addTo + 1;
        addWithDefault();
        addWithDefault( 5 );

        var counter = ( params int[] xs ) => xs.Length;
        counter();
        counter( 1, 2, 3 );

        var addWithDefault2 = AddWithDefaultMethod;
        addWithDefault2();
        addWithDefault2( 5 );

        var counter2 = CountMethod;
        counter2();
        counter2( 1, 2 );

        int AddWithDefaultMethod( int addTo = 2 )
        {
            return addTo + 1;
        }

        int CountMethod( params int[] xs )
        {
            return xs.Length;
        }
    }
}

#endif