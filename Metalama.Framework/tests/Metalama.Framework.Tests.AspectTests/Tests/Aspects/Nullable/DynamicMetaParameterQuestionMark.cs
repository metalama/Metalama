#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicMetaParameterQuestionMark;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Target.Parameters[0].Value?.ToString();
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

        [Aspect]
        private void Generic<T>( T t ) { }

        [Aspect]
        private void NullableGeneric<T>( T? t ) { }

        [Aspect]
        private void NotNullGeneric<T>( T t ) where T : notnull { }

        [Aspect]
        private void NullableNotNullGeneric<T>( T? t ) where T : notnull { }

        [Aspect]
        private void ValueTypeGeneric<T>( T t ) where T : struct { }

        [Aspect]
        private void NullableValueTypeGeneric<T>( T? t ) where T : struct { }

        [Aspect]
        private void ReferenceTypeGeneric<T>( T t ) where T : class { }

        [Aspect]
        private void NullableReferenceTypeGeneric<T>( T? t ) where T : class { }

        [Aspect]
        private void ReferenceTypeNullableGeneric<T>( T t ) where T : class? { }

        [Aspect]
        private void NullableReferenceTypeNullableGeneric<T>( T? t ) where T : class? { }

        [Aspect]
        private void SpecificReferenceTypeGeneric<T>( T t ) where T : IComparable { }

        [Aspect]
        private void SpecificNullableReferenceTypeGeneric<T>( T? t ) where T : IComparable { }

        [Aspect]
        private void SpecificReferenceTypeNullableGeneric<T>( T t ) where T : IComparable? { }

        [Aspect]
        private void SpecificNullableReferenceTypeNullableGeneric<T>( T? t ) where T : IComparable? { }
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
        private void Generic<T>( T t ) { }

        [Aspect]
        private void ValueTypeGeneric<T>( T t ) where T : struct { }

        [Aspect]
        private void NullableValueTypeGeneric<T>( T? t ) where T : struct { }

        [Aspect]
        private void ReferenceTypeGeneric<T>( T t ) where T : class { }

        [Aspect]
        private void SpecificReferenceTypeGeneric<T>( T t ) where T : IComparable { }
    }
}