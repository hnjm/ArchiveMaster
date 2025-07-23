dotnet publish ArchiveMaster.UI.Desktop -c Release -r win-x64 -p:PublishAot=true -p:PublishTrimmed=true -p:TrimMode=partial -o ./Publish/win-x64-aot
Remove-Item ./Publish/win-x64-aot/*.pdb