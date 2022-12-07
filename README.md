# NUTService
Windows service to communicate with NUT server (Like NUTClient on linux)

## Configuration
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

| variable name | description |
|---|---|
| host | host address of the NUT server, can be his FQDN or IP |
| ups | name of the ups to use |
| username | username to use for communication |
| password | password to use for communication |
| grace_delay | time before the system ois stopped, the user on the system will use this time to close their application |
| shutdown_on_low_battery | by default NUTService wait for FSD from NUT server to shutdown the system, if thus variable is true the shutdown will append on low battery notification from NUT |
