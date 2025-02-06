// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

// ReSharper disable once UnusedTypeParameter : I think it's used as a generic marker.
internal sealed class FormatterConverter<TTarget, TSource> : FormatterConverter<TTarget>
{
    public FormatterConverter( IFormatter wrapped, IFormatterRepository repository ) : base( wrapped, repository ) { }
}