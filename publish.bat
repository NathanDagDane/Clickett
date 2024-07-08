dotnet publish -c Release -r win-x64 -o Builds\x64 --self-contained false
vpk download http --url https://github.com/NathanDagDane/Clickett/releases/download/latest --channel app-x64
vpk pack --packId Clickett --packDir .\Builds\x64 --mainExe Clickett.exe --packTitle Clickett --icon .\res\iconcirc.ico --runtime win10.0.19041-x64 --channel app-x64 -o .\Releases\x64 --packVersion %1
del ".\Releases\x64\RELEASES-app-x64"

dotnet publish -c Release -r win-arm64 -o Builds\arm64 --self-contained false
vpk download http --url https://github.com/NathanDagDane/Clickett/releases/download/latest --channel app-arm64
vpk pack --packId Clickett --packDir .\Builds\arm64 --mainExe Clickett.exe --packTitle Clickett --icon .\res\iconcirc.ico --runtime win10.0.19041-arm64 --channel app-arm64 --outputDir .\Releases\ARM64 --packVersion %1
del ".\Releases\ARM64\RELEASES-app-arm64"