#!/bin/bash

dotnet build -c Release ./Zen.Logging.csproj
dotnet pack -c Release ./Zen.Logging.csproj

export PYTHON_SCRIPT=read_proj_version.py

export PROJ_FILE="./Zen.Logging.csproj"

export VERSION=$(python3 $PYTHON_SCRIPT $PROJ_FILE)

echo "$VERSION"

dotnet nuget push ./bin/Release/Zen.Logging.$VERSION.nupkg --skip-duplicate --api-key $DBACCESS_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
