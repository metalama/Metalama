// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Uri = System.Uri;

namespace Metalama.Backstage.Desktop.Windows;

public partial class WebBrowserWindow
{
    public WebBrowserWindow()
    {
        this.InitializeComponent();
    }

    [UsedImplicitly]
    public Uri Url
    {
        get => this.webView.Source;
        set => this.webView.Source = value;
    }
}