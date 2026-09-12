// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Authoring forms of a record, given to
/// <see cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration names what an author chooses when introducing a record. It is not reported by the code model:
/// a record read from source is a class or a struct according to <see cref="IType.TypeKind"/>, and
/// <see cref="INamedType.IsRecord"/> states that it is a record.
/// </para>
/// </remarks>
[CompileTime]
public enum RecordKind
{
    /// <summary>
    /// The type is not a record. This is the default value of the enumeration, and it is not a form that
    /// <see cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceRecord"/> accepts.
    /// </summary>
    None = 0,

    /// <summary>
    /// The record is a reference type, declared as <c>record</c> or <c>record class</c>. This is the form that
    /// <see cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceRecord"/> introduces by default.
    /// </summary>
    Class,

    /// <summary>
    /// The record is a value type, declared as <c>record struct</c>.
    /// </summary>
    Struct
}
