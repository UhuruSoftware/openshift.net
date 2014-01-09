# Updating dev box bits with code changes #
These notes describe a few steps required to update an openshift-origin VM with new code.

## Prerequisites ##

We need to add a dependency repo to yum and clone Uhuru's origin-server fork and the dev tools from openshift.

    yum install yum-utils
    yum-config-manager --add-repo https://mirror.openshift.com/pub/origin-server/release/2/fedora-19/dependencies/x86_64/
    yum-config-manager --add-repo https://mirror.openshift.com/pub/openshift-origin/nightly/fedora-19/dependencies/x86_64/
    yum install -y git vim rubygem-thor rubygem-parseconfig tito make rubygem-aws-sdk tig mlocate bash-completion rubygem-yard rubygem-redcarpet ruby-devel redhat-lsb
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

## Manually updating the sources ##

Clone the Uhuru origin-server repo somewhere (~/code/uhuru) and run this script:

	#!/bin/bash
	
	\cp "./origin-server/controller/app/models/application.rb"                                                      `ls /usr/share/gems/gems/openshift-origin-controller-*/app/models/application.rb` --backup=numbered -fr
	\cp "./origin-server/controller/app/models/gear.rb"                                                             `ls /usr/share/gems/gems/openshift-origin-controller-*/app/models/gear.rb` --backup=numbered -fr
	\cp "./origin-server/controller/app/models/group_instance.rb"                                                   `ls /usr/share/gems/gems/openshift-origin-controller-*/app/models/group_instance.rb` --backup=numbered -fr
	\cp "./origin-server/controller/app/models/pending_ops/create_group_instance_op.rb"                             `ls /usr/share/gems/gems/openshift-origin-controller-*/app/models/pending_ops/create_group_instance_op.rb` --backup=numbered -fr
	\cp "./origin-server/controller/app/helpers/cartridge_cache.rb"                                                 `ls /usr/share/gems/gems/openshift-origin-controller-*/app/helpers/cartridge_cache.rb` --backup=numbered -fr
	\cp "./origin-server/controller/lib/openshift/application_container_proxy.rb"                                   `ls /usr/share/gems/gems/openshift-origin-controller-*/lib/openshift/application_container_proxy.rb` --backup=numbered -fr
	\cp "./origin-server/plugins/msg-broker/mcollective/lib/openshift/mcollective_application_container_proxy.rb"   `ls /usr/share/gems/gems/openshift-origin-msg-broker-mcollective-*/lib/openshift/mcollective_application_container_proxy.rb` --backup=numbered -fr
	\cp "./origin-server/controller/app/models/pending_ops/init_gear_op.rb"                                         `ls /usr/share/gems/gems/openshift-origin-controller-*/app/models/pending_ops/init_gear_op.rb` --backup=numbered -fr
  