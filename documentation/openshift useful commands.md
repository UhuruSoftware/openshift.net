## Useful info for OpenShift dev environment ##

1. Restarting the Broker

    service openshift-broker restart
2. Restarting the Node:

    service mcollective restart
3. List all services on RHEL

    systemctl list-units
4. Clear the OpenShift Rails Cache

	`(cd /var/www/openshift/broker/; bundle exec rake tmp:clear)`

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
 - Application location: **/var/lib/openshift/**

7. Fix the iptables problem (for adding service cartridges)

Add the following in `/etc/sysconfig/iptables`

	*filter
	
	:INPUT ACCEPT [0:0]
	:FORWARD ACCEPT [0:0]
	:OUTPUT ACCEPT [0:0]
	:rhc-app-table - [0:0]
	:rhc-app-comm - [0:0]
	-A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT
	-A INPUT -p icmp -j ACCEPT
	COMMIT


8. Disable avahi plugin for broker (it can cause problems when creating and deleting applications if not configured properly)

`mv /etc/openshift/plugins.d/openshift-origin-dns-avahi.conf /etc/openshift/plugins.d/openshift-origin-dns-avahi._conf` 
 