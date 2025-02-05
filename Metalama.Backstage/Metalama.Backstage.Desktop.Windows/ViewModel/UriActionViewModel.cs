// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Desktop.Windows.ViewModel;

internal record UriActionViewModel( string Text, Uri Uri ) : NotificationActionViewModel( Text )
{
    public UriActionViewModel( string text, string uri ) : this( text, new Uri( uri ) ) { }
}