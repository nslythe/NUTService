# NUTService
Windows service to communicate with NUT server (like NUTClient on linux) and initiate a safe shutdown when the UPS force a shutdown or low battery according to your settings.

## Configuration
At startup if no config file `config.json` is present in the installation directory `C:\Program Files (x86)\NUTService` a new template file named `config.json.template` will be created by the service.

You can edit this file using the instruction below and rename the file `config.json`, the service will automatically reload the configuration file.

**_NOTE:_** The configuration file is monitored by NUTService, any change made to it will be reloaded automatically by the service, no need to restart the service between changes.

```
{
  "host": "192.168.0.1",  
  "ups": "ups",
  "username": "user",
  "password": "pass",
  "grace_delay": 30,
  "shutdown_on_low_battery": false
}
```

| Variable | Description |
|---|---|
| host | The host address of the NUT server, can be his FQDN or IP |
| ups | The name of the ups to use |
| username | Username to use for communication |
| password | Password to use for communication |
| grace_delay | Time before the system is shutdown, the user on the system will use this time to close their applications |
| shutdown_on_low_battery | By default NUTService wait until FSD is set from the NUT server to shutdown the system, if this variable is true the shutdown will append earlier on low battery notification from NUT |

## Logs
All logs for this service are in windows event logs in the log names NUTService.

## Link
- [NUT](https://networkupstools.org/)
