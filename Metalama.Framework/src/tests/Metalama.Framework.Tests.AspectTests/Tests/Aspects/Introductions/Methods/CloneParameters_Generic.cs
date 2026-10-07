// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Introductions.Methods.CloneParameters_Generic;

/*
 * Copies the type parameters and the parameters of a generic method into an introduced method. The types of the copied parameters and the
 * copied constraints refer to the copies of the type parameters, including a constraint that refers to a type parameter copied after it.
 */

public class CopyAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var source = builder.Target.Methods.OfName( "Source" ).Single();

        builder.IntroduceMethod(
            nameof(Template),
            buildMethod: method =>
            {
                method.Name = "Copy";

                foreach ( var typeParameter in source.TypeParameters )
                {
                    method.AddTypeParameter( typeParameter );
                }

                foreach ( var parameter in source.Parameters )
                {
                    method.AddParameter( parameter );
                }
            } );
    }

    [Template]
    public void Template()
    {
        foreach ( var parameter in meta.Target.Parameters )
        {
            Console.WriteLine( $"{parameter.Name}: {parameter.Type.ToTypeOfExpression().Value}" );
        }
    }
}

// <target>
[Copy]
public class Target
{
    public void Source<T, U>( List<T> items, T[] array, U? extra, IComparer<T>? comparer, Dictionary<T, U[]> map )
        where T : IComparer<U>
        where U : class { }
}
