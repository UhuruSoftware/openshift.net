#!/bin/bash
mkdir -p "$OPENSHIFT_DATA_DIR/.ssh/"
touch "$OPENSHIFT_DATA_DIR/.ssh/config"
ssh -o 'StrictHostKeyChecking=no' -o "UserKnownHostsFile=$OPENSHIFT_DATA_DIR/.ssh/known_hosts" -F "$OPENSHIFT_DATA_DIR/.ssh/config" "$@"