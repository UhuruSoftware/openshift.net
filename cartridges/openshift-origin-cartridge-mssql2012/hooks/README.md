Contains publish/subscribe events dispatched to other cartridges in an application.

## Cartridge Event Publishing ##
Publish events are defined via the **manifest.yml** for the cartridge, in the following format:
  `Publishes:   <event name>:     Type: "<event type>"`

When a cartridge is added to an application, each entry in the Publishes section of the manifest is used to construct events dispatched to other cartridges in the application. For each publish entry, OpenShift will attempt to execute a script named **hooks/<event name>**:
  `hooks/<event name> <gear name> <namespace> <gear uuid>`

All lines of output (on stdout) produced by the script will be joined by single spaces and used as the input to matching subscriber scripts. All cartridges which declare a subscription whose **Type** matches that of the publish event will be notified.

## Cartridge Event Subscriptions ##
Subscriptions to events published by other carts are defined via the manifest.yml for the cartridge, in the following format:
  `Subscribes:   <event name>     Type: "<event type>"`
  
When a cartridge publish event is fired, the subscription entries in the Subscribes section whose Type matches that of the publish event will be processed. Subscriptions which have a Type that starts with ENV: are processed differently, as described below. For each matching subscription event, OpenShift will attempt to execute a script named **hooks/<event name>**:
  `hooks/<event name> <gear name> <namespace> <gear uuid> <publish output>`