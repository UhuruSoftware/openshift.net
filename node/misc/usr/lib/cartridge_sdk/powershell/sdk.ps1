# report message on stderr and exit with provided exit status
# TODO We must improve this so that the string is sent to stderr
function error ($message, $code)
{
  $Host.ui.WriteErrorLine($message)
  exit $code
}

# report message on stderr
# TODO We must improve this so that the string is sent to stderr
function warning($message)
{
  $Host.ui.WriteErrorLine($message)
}

# report text to application developer
# Argument(s):
# - Text to be displayed on success
function client_result ($text)
{
   client_out "CLIENT_RESULT" $text 
}

# report text to application developer
# Argument(s):
# - Text will always be displayed. Used to notify application developer of a transient issue.
function client_message ($text)
{
   client_out "CLIENT_MESSAGE" $text 
}


# report text to application developer as error
# Argument(s):
# - Text will be displayed when there is an error
function client_error ($text)
{
    client_out "CLIENT_ERROR" $text 
}

# report text to application developer as error in your code
# Will be displayed...
function client_internal_error($text)
{
    client_out "CLIENT_INTERNAL_ERROR" $text 
}

# report text to application developer as debugging information
function client_debug($text)
{
     client_out "CLIENT_DEBUG" $text
}

# format text for reporting to application developer
#
# Argument(s):
# - type of message, will be prefix for each line in text
# - text to be processed
function client_out($type, $output) 
{
    $output -split "`r`n" | ForEach-Object {
        $text = "{0}: $_" -f $type
        Write-Host $text
    }
}

# set application information in Broker data store
# Argument(s):
# - name of attribute to add plus value
function set_app_info ($text)
{
    echo "APP_INFO: $text"
}

# set cartridge attribute in Broker data store
# Argument(s):
# - name of attribute to add plus value
function send_attr ($atribute)
{
    echo "ATTR:$atribute"
}

function add_domain_ssh_key ($key)
{
    echo "SSH_KEY_ADD:$key"
}

function add_app_ssh_key ($key1, $key2)
{
    echo "APP_SSH_KEY_ADD: $key1, $key2"
}

# Add environment variable visible to all gears in a domain
# Argument(s):
# - name of environment variable to add plus value
function add_domain_env_var ($env_var) 
{
    echo "ENV_VAR_ADD: $env_var"
}

# remove environment variable visible to all gears in application
# Argument(s):
# - name of environment variable to remove
function app_remove_env_var ($env_var)
{
    echo "APP_ENV_VAR_REMOVE: $env_var" 
}

function add_broker_auth_key 
{
    echo "BROKER_AUTH_KEY_ADD: "  
}

function remove_broker_auth_key 
{
    echo "BROKER_AUTH_KEY_REMOVE: "
}

# add cartridge data in Broker data store
# Argument(s):
# - list of cartridge datums
function cart_data 
{
    $result = ""
    $args | ForEach-Object { $result = "$_ $result" }
    echo "CART_DATA: $result"
}

# add cartridge properties in Broker data store
# Argument(s):
# - list of cartridge properties
function cart_props 
{
    $result = ""
    $args | ForEach-Object { $result = "$_ $result" }
    echo "CART_PROPERTIES: $result" 
}

# Sets the appropriate env variable files
# Arguments:
#  - Variable to set
#  - Value
#  - Target ENV directory
function set_env_var($var, $val, $target)
{
    if (([string]::IsNullOrEmpty($var)) -or ([string]::IsNullOrEmpty($val)) -or ([string]::IsNullOrEmpty($target)))
    {
        error "Must provide a variable name, value, and target directory for environment variables" 64
    }
    if ((Test-Path $target) -ne $true)
    {
        error "Target directory must exist for environment variables" 64
    }
    echo "$val" > "$target/$var"
}

# Generate a random string from /dev/urandom
# Arguments:
#  - Desired length (optional)
#  - Possible character space (optional)
#  - Patterns to omit (optional)
function random_string($len, $space, $omit)
{
    if ([string]::IsNullOrEmpty($len))
    {
        $len = 12
    }
    if ([string]::IsNullOrEmpty($space))
    {
        #TODO: We need to improve this by alowing ranges, linux example a-zA-Z0-9
        $space =  $NULL;For ($a=48;$a –le 122;$a++) {
        if (($a -le 57) -or (($a -gt 64) -and ($a -le 90) -or ($a -gt 96)))
            { 
                $space+=,[char][byte]$a 
            }
        }
    }
    if ([string]::IsNullOrEmpty($omit))
    {
        $omit = " "
    }

    do 
    {
        $rnd = ""
        for ($loop = 1; $loop -le [int]$len; $loop++)
        {

            $rnd += ([char[]]$space|Get-Random)
        } 

        
    }
    while ($rnd.Contains($omit))
    echo $rnd

}


# Pad a string with random characters
# Arguments:
#  - String to pad
#  - Desired length
#  - Pattern to pad with (optional)
function pad_string($str, $len, $pattern)
{
    $Local:remain = ([int]$len) - ($str.Length)
    if ($Local:remain -ge 1)
    {
        $Local:rnstr = random_string $Local:remain $pattern
        $str = "$str$Local:rnstr"
    }

    echo $str
}

# Generate a password
# Arguments:
#  - Desired length (optional)
#  - Character space (optional)
#  - Ignore pattern (optional)
function generate_password ($len, $space, $omit)
{
    if ([string]::IsNullOrEmpty($len))
    {
        $len = 12
    }

    if ([string]::IsNullOrEmpty($space))
    {
       #Dash, underscore, Alphanumeric except o,O,0
       $space =  $NULL;For ($a=49;$a –le 122;$a++) {
        if (($a -le 57) -or (($a -gt 64) -and ($a -le 78) -or (($a -gt 79) -and ($a -le 90)) -or (($a -gt 96) -and ($a -le 110)) -or ($a -gt 111)))
            { 
                $space+=,[char][byte]$a 
            }
        } 
        $space+=,[char]"_"
        $space+=,[char]"-"
    }
    if ([string]::IsNullOrEmpty($omit))
    {
        $omit = "^-"
    }
    
    echo (random_string $len $space $omit)
}

# Generate a username and pad it to a certain length
# Arguments:
#  - Username (optional)
#  - Desired length (optional)
#  - Pad characters (optional)
function generate_username($username, $len, $space)
{
    if ([string]::IsNullOrEmpty($username))
    {
        $username = "admin"
    }
    if ([string]::IsNullOrEmpty($len))
    {
        $len = 12
    }

    if ([string]::IsNullOrEmpty($space))
    {
       #Dash, underscore, Alphanumeric except o,O,0
       $space =  $NULL;For ($a=49;$a –le 122;$a++) {
        if (($a -le 57) -or (($a -gt 64) -and ($a -le 78) -or (($a -gt 79) -and ($a -le 90)) -or (($a -gt 96) -and ($a -le 110)) -or ($a -gt 111)))
            { 
                $space+=,[char][byte]$a 
            }
        } 
    }
    
    echo (pad_string $username $len $space)
}

# wait up to 30 seconds for given process to stop
# Argument(s):
#  - process id to wait on
function wait_for_stop 
{
    Throw "Not implemented"
}

# report processing running for given user uid.
# Argument(s):
# - user uid    Do not use user login name, all numeric user login names will break ps
function print_user_running_processes 
{
    Throw "Not implemented"
}

# Check is a process is running
# Arguments:
#  - Process name
#  - Pidfile
function process_running($processName, $pidFile)
{
    if ((Test-Path $pidFile) -eq $false)
    {
        return $false
    }

    $processId = Get-Content $pidFile -ErrorAction SilentlyContinue

    $process = Get-Process | Where-Object {($_.id -eq $processId.ToString().Trim()) -and ($_.ProcessName -eq $processName)}
    
    return ($process -ne $null)
}

function pid_is_httpd() 
{
    Throw "Not implemented"
}

function killall_matching_httpds() 
{
    Throw "Not implemented"
}

# Attempt to resurrect the Apache PID file if its corrupt.
#  Caution: there may be multiple Apache processes on the gear.
function ensure_valid_httpd_pid_file() 
{
    Throw "Not implemented"
}

function ensure_valid_httpd_process() 
{
    Throw "Not implemented"
}


# application developer has requested a hot deploy, 0 == true, 1 == false
function hot_deploy_marker_is_present() 
{
    Throw "Not implemented"
}

# report the primary cartridge name for this gear
function primary_cartridge_name() 
{
    Throw "Not implemented"
}

# Returns 0 if the named marker $1 exists, otherwise 1.
function marker_present() 
{
    Throw "Not implemented"
}

# Add element(s) to end of path
#
# $1 path
# $2 element(s) to add
# return modified path
function path_append 
{
    Throw "Not implemented"
}

# Add element(s) to front of path
#
# $1 path
# $2 element(s) to add
# return modified path
function path_prepend 
{
    Throw "Not implemented"
}

# Remove element(s) from path
#
# $1 path
# $2 element(s) to remove
# return modified path
function path_remove 
{
    Throw "Not implemented"
}


# Update the PassEnv directives in the httpd configuration file
#
# $1 full path to httpd.conf file
function update_httpd_passenv 
{
    Throw "Not implemented"
}

# Sync repo to the other gears of this app
#
# $1 Flag to indicate whether this is a new gear
# $2 An array of gears to sync
#    Format:  (<gear_uuid>@<ip>:<deploy_cart_type>;<gear_name>-<namespace>.<openshift_domain> <gear_uuid>@<ip>:<deploy_cart_typ$
#        Ex:  (e86e4bb0e37111e29c0112313d157058@10.85.127.166:php;e86e4bb0e37111e29c0112313d157058-myns.rhcloud.com 51d367f4af9$
# return the combined exit code of the sync operation (non-zero means something failed)
function sync_gear_repos 
{
    Throw "Not implemented"
}


