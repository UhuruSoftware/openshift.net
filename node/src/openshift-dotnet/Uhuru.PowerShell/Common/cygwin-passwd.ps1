

function Get-SSHDUsers($cygwinPath)
{
    $passwdFile = Join-Path $cygwinPath "etc\passwd"
    
    $users = @{}
    
    Get-Content $passwdFile | ForEach-Object {
        $user = $_.Split(':')
        
        if ($user.Length -eq 7)
        {
            $windowsUser = $user[4].Split(',')[0]
            $windowsUserSID = $user[4].Split(',')[1]
            
            $users[$windowsUser] = @{
                "user" = $user[0];
                "uid" = $user[2];
                "gid" = $user[3];
                "sid" = $windowsUserSID;
                "home" = $user[5];
                "shell" = $user[6];
            }
        }
    }
    
    $users
}

function Get-SSHDUser($cygwinPath, $windowsUser)
{
    (Get-SSHDUsers $cygwinPath)[$windowsUser]
}


function Get-NoneGroupSID()
{
    try
    {
        $objUsersGroup = New-Object System.Security.Principal.NTAccount('None')
        $strSID = $objUsersGroup.Translate([System.Security.Principal.SecurityIdentifier])
        $strSID.Value
    }
    catch
    {
        Write-Host "Could not get SID for the local 'None' group. Aborting." -ForegroundColor Red
        exit 1
    }
}
