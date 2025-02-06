// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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