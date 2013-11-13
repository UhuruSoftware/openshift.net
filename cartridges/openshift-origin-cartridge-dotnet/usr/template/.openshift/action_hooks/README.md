Contains code the application developer wishes to be run during lifecycle changes. Examples would be:

`pre_start_"cartridge name"`
`post_start_"cartridge name"`
`pre_stop_"cartridge name"`
`...

As a cartridge author you do not need to execute the default action_hooks. OpenShift will call them during lifecycle changes based on the actions given to the control script. If you wish to add additional hooks, you are expected to document them and you will need to run them explicitly in your control script.