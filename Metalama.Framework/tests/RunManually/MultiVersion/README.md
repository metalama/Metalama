This test verifies that several versions of Metalama can be used in the same solution and that a project can use aspects coming from libraries with different versions of Metalama.

To run the test manually, do this in PowerShell starting from `c:\src\Metalama`:

1. Do two different builds of Metalama with the _same_ version of Metalama.Compiler:

    ```powershell
    b build
    md other-version
    move .\artifacts\publish\private\* .\other-version\
    b build
    ```

2. Build and run the test project:

    ```powershell
    cd RunManually\MultiVersion
    dotnet run --project .\Version2\Version2.csproj
    ```

The build should be successful and the output should look like this:

```txt
Aspect1 on Program.Main() compiled with Metalama.Framework, Version=0.5.74.1644, Culture=neutral, PublicKeyToken=772fca7b1db8db06
Aspect2 on Program.Main() compiled with Metalama.Framework, Version=0.5.74.1644, Culture=neutral, PublicKeyToken=772fca7b1db8db06
Aspect1 on Class1.Method1() compiled with Metalama.Framework, Version=0.5.74.1643, Culture=neutral, PublicKeyToken=772fca7b1db8db06
```

The aspects on `Program.Main` should be of the higher version, the aspect on `Class1.Method1` should be of the lower version.

If the three lines show the same version, the test should be considered failed.









