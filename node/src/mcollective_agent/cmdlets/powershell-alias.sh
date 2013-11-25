#!/usr/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

FILES=${DIR}/*.ps1

for file in ${FILES}
do
    file_name=$(basename ${file})
    alias_name=$(echo "${file_name}" | sed -e 's/\(\.ps1\)*$//g')
    windows_path=$(cygpath -m ${file})
    eval "alias ${alias_name}='powershell -File ${windows_path}'"
done