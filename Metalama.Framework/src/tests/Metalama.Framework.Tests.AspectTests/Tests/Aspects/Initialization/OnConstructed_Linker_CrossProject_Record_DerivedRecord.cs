// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Linker_CrossProject_Record_DerivedRecord;

// Both base and derived are records; non-inheritable aspect on base.
// Derived primary ctor gets the epilogue + `context.Descend(OnConstructed)` rewrite on the
// `:base(X)` call; `Deconstruct` regeneration happens in the dependent project via the
// linker's primary-ctor parameter-append path.

// <target>
public record DerivedRecord( int X, int Y ) : BaseRecord( X );
