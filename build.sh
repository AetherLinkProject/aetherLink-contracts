#!/bin/bash

set -e

ContractsName=$1
VERSION=$2

SLN=$3

[ -z ${VERSION} ] && { echo "Usage: $0 <version>"; exit 1; }

[ -d "/opt/build/${ContractsName}" ] && rm -rf /opt/build/${ContractsName}/*

cd /opt/contracts

/bin/bash -x scripts/download_binary.sh

dotnet restore ${SLN}

if [ x"${ContractsName}" = x"all" ]; then
  for NAME in $(ls contract);
  do
    if [ -d "contract/${NAME}" ]; then
      dotnet publish \
        contract/${NAME}/${NAME}.csproj \
        /p:NoBuild=false \
        /p:Version=${VERSION} \
        -c Release \
        -o /opt/build/${NAME}-${VERSION}
    fi
  done
else
  dotnet publish \
    contract/${ContractsName}/${ContractsName}.csproj \
    /p:NoBuild=false \
    /p:Version=${VERSION} \
    -c Release \
    -o /opt/build/${ContractsName}-${VERSION}
fi