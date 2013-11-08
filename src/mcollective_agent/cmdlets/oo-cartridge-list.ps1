$cartridge = @"
---
Name: dotnet
Display-Name: .NET
Version: '4.0'
Versions:
- '4.0'
Description: The .NET Framework is a software framework developed by Microsoft that runs primarily on Microsoft Windows. 
License: TODO: add license
License-Url: TODO: add license URL
Categories:
- service
- dotnet
- web_framework
Website: http://www.microsoft.com/net
Help-Topics:
  Developer Center: http://www.microsoft.com/net
Cart-Data:
- Key: OPENSHIFT_TMP_DIR
  Type: environment
  Description: Directory to store application temporary files.
- Key: OPENSHIFT_REPO_DIR
  Type: environment
  Description: Application root directory where application files reside. This directory is reset every time you do a git-push
- Key: OPENSHIFT_APP_PORT
  Type: environment
  Description: Internal port to which the web-framework binds to.
- Key: OPENSHIFT_APP_IP
  Type: environment
  Description: Internal IP to which the web-framework binds to.
- Key: OPENSHIFT_APP_DNS
  Type: environment
  Description: Fully qualified domain name for the application.
- Key: OPENSHIFT_APP_NAME
  Type: environment
  Description: Application name
- Key: OPENSHIFT_DATA_DIR
  Type: environment
  Description: Directory to store application data files. Preserved across git-pushes. Not shared across gears.
- Key: OPENSHIFT_APP_UUID
  Type: environment
  Description: Unique ID which identified the application. Does not change between gears.
- Key: OPENSHIFT_GEAR_UUID
  Type: environment
  Description: Unique ID which identified the gear. This value changes between gears.
Additional-Control-Actions:
- threaddump
Provides:
- dotnet-4.0
- dotnet
- dotnet(version) = 4.0
Vendor: microsoft
Cartridge-Vendor: uhuru
Endpoints:
- Private-IP-Name: IP
  Private-Port-Name: PORT
  Private-Port: 8080
  Public-Port-Name: PROXY_PORT
  Mappings:
  - Frontend: ''
    Backend: ''
    Options:
      websocket: true
  - Frontend: /health
    Backend: ''
    Options:
      health: true
Publishes:
  publish-http-url:
    Type: NET_TCP:httpd-proxy-info
    Required: false
  publish-gear-endpoint:
    Type: NET_TCP:gear-endpoint-info
    Required: false
Subscribes:
  set-env:
    Type: ENV:*
    Required: false
  set-mysql-connection-info:
    Type: NET_TCP:db:mysql
    Required: false
  set-postgres-connection-info:
    Type: NET_TCP:db:postgres
    Required: false
  set-doc-url:
    Type: STRING:urlpath
    Required: false
Scaling:
  Min: 1
  Max: -1
  Min-Managed: 0
  Multiplier: 1
"@
$json = ConvertTo-Json @($cartridge) -Compress
write-host "CLIENT_RESULT: $json"
exit 0