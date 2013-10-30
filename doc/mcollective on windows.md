# Notes for setting up The Marionette Collective on Windows #


- All machines that use mcollective have to be clock synchronized (including timezone)
- It has to be installed in c:\mcollective 
- The direct_addressing setting has to be set to 1 so the node can be discovered from a Linux machine

**The following notes are specific to running mcollective in the context of an OpenShift deployment.**

- The OpenShift agent has a custom validator (any\_validator.rb and any\_validator.ddl) which needs to be present in C:\mcollective\plugins\mcollective\validator