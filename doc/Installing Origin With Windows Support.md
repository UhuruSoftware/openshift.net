#Origin Installation (with Windows support)

Start with a Centos 6.5 minimal installation. For more details you can check out this: http://openshift.github.io/documentation/oo_install_users_guide.html

###Install EPEL

	yum install wget -y 
	wget http://dl.fedoraproject.org/pub/epel/6/x86_64/epel-release-6-8.noarch.rpm
	rpm -Uvh epel-release-6*.rpm

####Add dependencies repo
 
	cat > /etc/yum.repos.d/openshift-origin-deps.repo <<"EOF"
	[openshift-origin-deps]
	name=OpenShift Origin Dependencies - EL6
	baseurl=http://mirror.openshift.com/pub/origin-server/release/3/rhel-6/dependencies/x86_64/
	gpgcheck=0
	EOF

###Install dependencies

	yum install ruby puppet ruby193-ruby unzip openssh-clients \
	augeas-1.0.0-5.el6_5.1.x86_64 install \
	32:bind-9.8.2-0.23.rc1.el6_5.1.x86_64 httpd-tools -y

###Install Origin

- Use one-liner from [install.openshift.com](http://install.openshift.com) installation 
	
		sh <(curl -s https://install.openshift.com/)

- **WHEN ASKED ABOUT MAKING CHANGES TO SUBSCRIPTION INFO**:
 - Say `yes`
 - Type of subscription is `yum`
 - Base URL for OpenShift repositories should be one of the following:
   - Uhuru's development RPM repo: `http://rpm.uhurucloud.net/origin-rpms/`
   - Origin nightly build: ``
   - Your own repo


###Post install

####Install `dbus` and enable some services

	yum install dbus -y
	chkconfig cgconfig on
	chkconfig cgred on

####Reboot the box

	reboot

####Edit `/etc/openshift/broker.conf` and set available platforms

	NODE_PLATFORMS="linux,windows"

####Run diagnostics to make sure there are no errors
Some warnings will be displayed.
 
	oo-diagnostics --verbose

###Import cartridges

Clear cartridges

	for i in `oo-admin-ctl-cartridge -c list|awk '{print $2}'`;do echo "$i";done | oo-admin-ctl-cartridge -c deactivate
	oo-admin-ctl-cartridge -c clean
	oo-admin-broker-cache --clear
	oo-admin-ctl-cartridge -c import-node --activate --force
