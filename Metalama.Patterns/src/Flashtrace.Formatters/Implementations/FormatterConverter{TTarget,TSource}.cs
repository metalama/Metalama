// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.Implementations;

// ReSharper disable once UnusedTypeParameter : I think it's used as a generic marker.
internal sealed class FormatterConverter<TTarget, TSource> : FormatterConverter<TTarget>
{
    public FormatterConverter( IFormatter wrapped, IFormatterRepository repository ) : base( wrapped, repository ) { }
}