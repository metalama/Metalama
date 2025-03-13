// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts.UnitTests.Assets;

public class NotNullTestClass
{
    [NotNull]
    public object ObjectField;

    [NotNull]
    public object ObjectProperty { get; set; }

    public object ObjectParameterMethod( [NotNull] object parameter ) => parameter;

    public object ClassParameterMethod( [NotNull] NotNullAttributeTests parameter ) => parameter;

    public class A;

    public class B<T>
        where T : A
    {
        public B( [NotNull] T x ) { }
    }
}