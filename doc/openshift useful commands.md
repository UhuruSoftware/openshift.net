## Useful info for OpenShift dev environment ##

1. Restarting the Broker

    service openshift-broker restart
2. Restarting the Node:

    service mcollective restart
3. List all services on RHEL

    systemctl list-units
4. Clear the OpenShift Rails Cache

	`cd /var/www/openshift/broker`

	`bundle exec rake tmp:clear`

5. Run irb in the context of a broker (get access to useful node management code)
 - go to **/var/www/openshift/broker**
 - `bundle exec irb`
 - `require "/var/www/openshift/broker/config/environment"`
 - `OpenShift::ApplicationContainerProxy.find_available`
6. Useful paths
 - Broker and Web Console: **/var/www/openshift**
 - Configuration files: **/etc/openshift/**
 - Mcollective Agent dir: **/usr/libexec/mcollective/mcollective/agent**
 - Cartridges location on node: **/usr/libexec/openshift/cartridges**
 - Location of ruby gems: **/usr/share/gems/gems/**