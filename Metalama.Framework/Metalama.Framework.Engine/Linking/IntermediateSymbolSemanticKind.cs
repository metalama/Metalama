// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Described semantics of a symbol in intermediate compilation. This allows to differentiate between versions of override targets.
/// Intermediate symbols are of the following categories:
///  1) Source member that does not hide or override a base member, no aspect overrides.
///  2) Source member hiding or overriding a base member, no aspect overrides.
///  3) Aspect-overridden source member that does not hide or override a base member.
///  4) Aspect-overridden source member hiding or overriding a base member.
///  5) Aspect-introduced member that does not hide or override a base member, no aspect overrides.
///  6) Aspect-introduced member hiding a base member, no aspect overrides.
///  7) Aspect-introduced and aspect-overriden member that does not hide or override a base member.
///  8) Aspect-introduced and aspect-overriden member hiding or overriding a base member.
///  
/// 7) and 8) are possible only for fields and event fields. All introduced declarations with bodies have at least one aspect override.
/// 
/// Symbols that represent overrides always use Default value.
/// </summary>
internal enum IntermediateSymbolSemanticKind : byte
{
    /// <summary>
    /// Default symbol semantic, i.e. version visible in the intermediate compilation.
    /// 1) this.Foo
    /// 2) this.Foo
    /// 3) this.Foo_Source
    /// 4) this.Foo_Source
    /// 5) this.Foo
    /// 6) this.Foo
    /// 7) invalid
    /// 8) invalid
    /// 
    /// 7) to 8) would mean empty introduced declaration which should not be directly referenced.
    /// </summary>
    Default,

    /// <summary>
    /// Base symbol semantic. Represents the previous version of the symbol before any aspect or the current type.
    ///  1) invalid
    ///  2) base.Foo
    ///  3) invalid
    ///  4) invalid
    ///  5) this.Foo_Empty
    ///  6) base.Foo
    ///  7) this.Foo_Empty
    ///  8) base.Foo
    /// 
    /// 1), 3) are invalid because the "base" version does not exist.
    /// 4) is invalid because aspect override cannot reference the base implementation of the source method.
    /// </summary>
    Base,

    /// <summary>
    /// Final symbol semantic. Represents the version of the symbol final from the point of view of the current class, including all aspects.
    ///  1) invalid
    ///  2) invalid
    ///  3) this.Foo
    ///  4) this.Foo
    ///  5) invalid
    ///  6) invalid
    ///  7) this.Foo
    ///  8) this.Foo
    ///  
    /// 1), 2), 5), 6) are invalid because there are not aspect overrides.
    /// </summary>
    Final
}