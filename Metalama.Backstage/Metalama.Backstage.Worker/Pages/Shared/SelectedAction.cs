// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Pages.Shared;

[PublicAPI]
public enum SelectedAction
{
    None,
    OpenSource,
    Trial,
    Register,
    Skip
}