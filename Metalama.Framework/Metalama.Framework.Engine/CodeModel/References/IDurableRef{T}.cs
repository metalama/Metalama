// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// An <see cref="IRef"/> that does not keep a reference to a <see cref="CompilationContext"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IDurableRef<out T> : IRef<T>, IDurableRef
    where T : class, ICompilationElement { }