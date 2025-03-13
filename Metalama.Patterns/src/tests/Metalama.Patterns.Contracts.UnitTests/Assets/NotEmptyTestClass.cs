// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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