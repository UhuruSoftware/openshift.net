# Updating dev box bits with code changes #
These notes describe a few steps required to update an openshift-origin VM with new code.

## Prerequisites ##

We need to add a dependency repo to yum and clone Uhuru's origin-server fork and the dev tools from openshift.

    yum install yum-utils
    yum-config-manager --add-repo https://mirror.openshift.com/pub/openshift-origin/nightly/fedora-19/dependencies/x86_64/
    mkdir ~/code
    cd ~/code
    git clone git@github.com:UhuruSoftware/origin-server.git
    git clone git@github.com:openshift/origin-dev-tools.git

Then we have to run some devenv commands to complete our development environment. 

    cd origin-dev-tools
    ./build/devenv clone_addtl_repos master
    ./build/devenv install_required_packages


## Repeat the following steps any time you need to make a change ##

    cd [changed component]
    tito tag
    cd ~/code/origin-dev-tools
    ./build/devenv local_build
    cd ~/origin-rpms
    createrepo .
    yum update --nogpgcheck
    cd ~/code/origin-dev-tools
    ./build/devenv restart_services
    openssl rsa -in /etc/openshift/server_priv.pem -pubout >/var/www/openshift/broker/config/server_pub.pem
    service avahi-cname-manager start 