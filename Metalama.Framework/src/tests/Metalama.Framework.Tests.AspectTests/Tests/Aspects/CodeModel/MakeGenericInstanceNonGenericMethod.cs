// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.MakeGenericInstanceNonGenericMethod;

/// <summary>
/// A generic type exposing a non-generic method, used as the subject of the diagnostic asserted by this test.
/// </summary>
/// <remarks>
/// The type is declared in the test compilation instead of being taken from the base class library because the
/// diagnostic message quotes the signature of the method, including its nullable annotations. The annotations of
/// a base class library method depend on the reference assemblies of the target framework, so a method such as
/// <c>EqualityComparer{T}.Equals</c> renders as <c>Equals(T?, T?)</c> under .NET and as <c>Equals(T, T)</c> under
/// .NET Framework, which no single expected output file can match.
/// </remarks>
internal class MyComparer<T>
{
    /// <summary>
    /// A non-generic method, which therefore cannot be used with <c>MakeGenericInstance</c>.
    /// </summary>
    public bool AreEqual( T x, T y ) => true;
}

public class MyAspect : TypeAspect
{
    [Introduce]
    public bool IntroducedEquals()
    {
        var comparerAreEqual = ((INamedType) TypeFactory.GetType( typeof(MyComparer<>) ))
            .Methods.OfName( "AreEqual" ).Single();

        foreach (var member in meta.Target.Type.AllFieldsAndProperties)
        {
            var equals = comparerAreEqual.MakeGenericInstance( member.Type );
        }

        return true;
    }
}

// <target>
[MyAspect]
internal class Target
{
    private int _i;
}
