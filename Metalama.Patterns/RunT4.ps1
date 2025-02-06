dir *.tt -Recurse | foreach { 
    echo "Transforming $_"
    & "C:\Program Files\Microsoft Visual Studio\2022\17.0\Common7\IDE\TextTransform.exe" $_ 
}