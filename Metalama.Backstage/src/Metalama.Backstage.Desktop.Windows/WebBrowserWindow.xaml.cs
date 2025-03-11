// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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