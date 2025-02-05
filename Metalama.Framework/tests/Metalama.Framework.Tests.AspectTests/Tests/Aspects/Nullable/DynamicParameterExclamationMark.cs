#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterExclamationMark;

internal class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    private void Template( dynamic? arg ) => arg!.ToString();
}

// <target>
internal class TargetCode
{
    private class Nullable
    {
        [Aspect]
        private void ValueType( int arg ) { }

        [Aspect]
        private void NullableValueType( int? arg ) { }

        [Aspect]
        private void ReferenceType( string arg ) { }

        [Aspect]
        private void NullableReferenceType( string? arg ) { }

        [Aspect]
        private void Generic<T>( T arg ) { }

        [Aspect]
        private void NullableGeneric<T>( T? arg ) { }

        [Aspect]
        private void NotNullGeneric<T>( T arg ) where T : notnull { }

        [Aspect]
        private void NullableNotNullGeneric<T>( T? arg ) where T : notnull { }

        [Aspect]
        private void ValueTypeGeneric<T>( T arg ) where T : struct { }

        [Aspect]
        private void NullableValueTypeGeneric<T>( T? arg ) where T : struct { }

        [Aspect]
        private void ReferenceTypeGeneric<T>( T arg ) where T : class { }

        [Aspect]
        private void NullableReferenceTypeGeneric<T>( T? arg ) where T : class { }

        [Aspect]
        private void ReferenceTypeNullableGeneric<T>( T arg ) where T : class? { }

        [Aspect]
        private void NullableReferenceTypeNullableGeneric<T>( T? arg ) where T : class? { }

        [Aspect]
        private void SpecificReferenceTypeGeneric<T>( T arg ) where T : IComparable { }

        [Aspect]
        private void SpecificNullableReferenceTypeGeneric<T>( T? arg ) where T : IComparable { }

        [Aspect]
        private void SpecificReferenceTypeNullableGeneric<T>( T arg ) where T : IComparable? { }

        [Aspect]
        private void SpecificNullableReferenceTypeNullableGeneric<T>( T? arg ) where T : IComparable? { }
    }

#nullable disable

    private class NonNullable
    {
        [Aspect]
        private void ValueType( int arg ) { }

        [Aspect]
        private void NullableValueType( int? arg ) { }

        [Aspect]
        private void ReferenceType( string arg ) { }

        [Aspect]
        private void Generic<T>( T arg ) { }

        [Aspect]
        private void ValueTypeGeneric<T>( T arg ) where T : struct { }

        [Aspect]
        private void NullableValueTypeGeneric<T>( T? arg ) where T : struct { }

        [Aspect]
        private void ReferenceTypeGeneric<T>( T arg ) where T : class { }

        [Aspect]
        private void SpecificReferenceTypeGeneric<T>( T arg ) where T : IComparable { }
    }
}