// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Patterns.Contracts.UnitTests.Assets;

public class NotEmptyTestClass
{
    [NotNull]
    [NotEmpty]
    public string StringField;

    [NotNull]
    [NotEmpty]
    public List<int> ListField;

    [NotNull]
    [NotEmpty]
    public ICollection<int> GenericCollectionProperty { get; set; }

    public string StringMethod( [NotNull] [NotEmpty] string parameter ) => parameter;

    public string StringMethod_WithNotNull( [NotNull] [NotEmpty] string parameter ) => parameter;

    public List<int> ListMethod( [NotEmpty] List<int> parameter ) => parameter;

    public void StringMethodWithRef( string newVal, [NotNull] [NotEmpty] ref string parameter ) => parameter = newVal;

    public void StringMethodWithOut( string newVal, [NotNull] [NotEmpty] out string parameter ) => parameter = newVal;

    [return: NotEmpty]
    [return: NotNull]
    public string StringMethodWithRetVal( string retVal ) => retVal;

    public void IReadOnlyCollectionMethod<T>( [NotNull] [NotEmpty] IReadOnlyCollection<T> parameter ) { }

    public void Array( [NotNull] [NotEmpty] int[] array ) { }

    public void ImmutableArray( [NotEmpty] ImmutableArray<int> array ) { }
}