# NUTService
Windows service to communicate with NUT server (Like NUTClient on linux) and initiate safe shutdown when UPS force shutdown or low battery depending on your settings.

## Configuration
At startup if no config file `config.json` is present in installation folder `C:\Program Files (x86)\NUTService` a new template file named `config.json.template` will be created by the service.

You can modify this file acording to the instruction here and rename the file `config.json`, the service will automaticaly relaod the config file.

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
| host | host address of the NUT server, can be his FQDN or IP |
| ups | name of the ups to use |
| username | username to use for communication |
| password | password to use for communication |
| grace_delay | time before the system ois stopped, the user on the system will use this time to close their application |
| shutdown_on_low_battery | by default NUTService wait for FSD from NUT server to shutdown the system, if this variable is true the shutdown will append on low battery notification from NUT |

## Logs
All logs for this service will be in windows event logs in the log names NUTService.

## Link
- [NUT](https://networkupstools.org/)
