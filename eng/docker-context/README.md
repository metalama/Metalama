To get an invariant version of VS Build Tools:
- download https://aka.ms/vs/17/release/channel and store it _unchanged_ (no formatting) under this directory
- find the first URL starting with `https://download.visualstudio.microsoft.com/` and paste it to Dockerfile as the `--installCatalogUri` parameter of `vs_buildtools.exe`.
- 

Hopefully Microsoft won't delete the download too soon.