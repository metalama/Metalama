// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicMetaParameterExclamationMark;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Target.Parameters[0].Value!.ToString();
}

// <target>
internal class TargetCode
{
    private class Nullable
    {
        [Aspect]
        private void ValueType( int i ) { }

        [Aspect]
        private void NullableValueType( int? i ) { }

        [Aspect]
        private void ReferenceType( string s ) { }

        [Aspect]
        private void NullableReferenceType( string? s ) { }

        // t1 has no nullability annotation and we cannot detect if ! is redundant
        // without more complex analysis.
        [Aspect]
        private void Generic<T>( T t1 ) { }

        [Aspect]
        private void NullableGeneric<T>( T? t2 ) { }

        [Aspect]
        private void NotNullGeneric<T>( T t3 ) where T : notnull { }

        [Aspect]
        private void NullableNotNullGeneric<T>( T? t4 ) where T : notnull { }

        [Aspect]
        private void ValueTypeGeneric<T>( T t5 ) where T : struct { }

        [Aspect]
        private void NullableValueTypeGeneric<T>( T? t6 ) where T : struct { }

        [Aspect]
        private void ReferenceTypeGeneric<T>( T t7 ) where T : class { }

        [Aspect]
        private void NullableReferenceTypeGeneric<T>( T? t8 ) where T : class { }

        [Aspect]
        private void ReferenceTypeNullableGeneric<T>( T t9 ) where T : class? { }

        [Aspect]
        private void NullableReferenceTypeNullableGeneric<T>( T? t10 ) where T : class? { }

        [Aspect]
        private void SpecificReferenceTypeGeneric<T>( T t11 ) where T : IComparable { }

        [Aspect]
        private void SpecificNullableReferenceTypeGeneric<T>( T? t12 ) where T : IComparable { }

        [Aspect]
        private void SpecificReferenceTypeNullableGeneric<T>( T t13 ) where T : IComparable? { }

        [Aspect]
        private void SpecificNullableReferenceTypeNullableGeneric<T>( T? t14 ) where T : IComparable? { }
    }

#nullable disable

    private class NonNullable
    {
        [Aspect]
        private void ValueType( int i ) { }

        [Aspect]
        private void NullableValueType( int? i ) { }

        [Aspect]
        private void ReferenceType( string s ) { }

        [Aspect]
        private void Generic<T>( T t15 ) { }

        [Aspect]
        private void ValueTypeGeneric<T>( T t16 ) where T : struct { }

        [Aspect]
        private void NullableValueTypeGeneric<T>( T? t17 ) where T : struct { }

        [Aspect]
        private void ReferenceTypeGeneric<T>( T t18 ) where T : class { }

        [Aspect]
        private void SpecificReferenceTypeGeneric<T>( T t19 ) where T : IComparable { }
    }
}