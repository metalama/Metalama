// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.TypeExtensions;

public delegate void TypeExtensionCacheUpdateCallback<T>( TypeExtensionInfo<T> typeExtension )
    where T : class;