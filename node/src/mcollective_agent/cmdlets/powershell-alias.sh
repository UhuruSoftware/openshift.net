#!/usr/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/../../powershell/oo-cmdlets" && pwd )"

FILES=${DIR}/*.ps1

mkdir -p ~/.oo-bin

for file in ${FILES}
do
    file_name=$(basename ${file})
    alias_name=$(echo "${file_name}" | sed -e 's/\(\.ps1\)*$//g')
    windows_path=$(cygpath -m ${file})
    echo "powershell -File ${windows_path} \$@" > ~/.oo-bin/${alias_name}
    chmod +x ~/.oo-bin/${alias_name}
done