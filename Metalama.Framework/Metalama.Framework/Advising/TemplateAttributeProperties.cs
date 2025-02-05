// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

[CompileTime]
public sealed record TemplateAttributeProperties(
    string? Name = null,
    Accessibility? Accessibility = null,
    bool? IsVirtual = null,
    bool? IsSealed = null,
    bool? IsRequired = null,
    bool? IsAbstract = null,
    bool? IsPartial = null,
    bool? IsExtern = null );