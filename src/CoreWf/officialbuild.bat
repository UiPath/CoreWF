dotnet msbuild CoreWf.csproj /p:Configuration=Release /p:OfficialBuild=true /t:Rebuild
dotnet pack /p:Configuration=Release /p:NuspecFile=CoreWf.nuspec /p:NuspecBasePath=.\pack /p:OfficialBuild=true
