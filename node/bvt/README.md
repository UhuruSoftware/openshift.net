# Build Verification Tests #

This folder contains a few build verification tests that are meant to check an OpenShift deployment that supports Windows cartridges.

## Running the tests ##

Before running the tests:

- make sure your `~/.ssh/config` file contains `StrictHostKeyChecking no`. This is needed to disable user interaction when cloning/ssh-ing to apps, otherwise they will hang.
- run `bundle exec rhc setup` at least once before running the tests, otherwise they will hang

To run the tests run the following (this is meant for a automated build environment, like Jenkins):
    
    export UHURU_KNOWN_HOSTS=~/.ssh/known_hosts
    export UHURU_CLOUD_HOST=[domain of cloud to be tested, e.g. openshift.com]
    export UHURU_SERVER=[broker host, e.g. openshift.redhat.com]
    export UHURU_DOMAIN_NAME=[your namespace, e.g. dev]
    export UHURU_RHLOGIN=[openshift username]
    export UHURU_PASSWORD=[openshift password]
    
    bundle install
    bundle exec rake spec SPEC_OPTS="--format documentation"
     