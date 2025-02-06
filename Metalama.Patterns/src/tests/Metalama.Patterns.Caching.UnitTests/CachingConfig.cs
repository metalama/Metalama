// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// Don't use dependency injection by default.

using Metalama.Patterns.Caching.Aspects;

[assembly: CachingConfiguration( UseDependencyInjection = false )]