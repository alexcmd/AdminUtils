# AdminUtils
Automotion administration utilities for cross-platform .net developing

# Using

## AdminUtils.Backup
Utility to backup mysql database to remote server

* Set up connection string "DefaultContext"
* If need upload dump to remote server set params for remote connections

### SSH example params
```
<appSettings>
      <add key="Path" value="c:\temp"/>
      <add key="RemoteServer" value="ssh://127.0.0.1"/>
      <add key="RemotePath" value="/home/login"/>
      <add key="UserName" value="login"/>
      <add key="Password" value="password"/>
</appSettings>
```

### FTP example params
```
<appSettings>
      <add key="Path" value="c:\temp"/>
      <add key="RemoteServer" value="ssh://127.0.0.1"/SomeDir">
      <add key="UserName" value="login"/>
      <add key="Password" value="password"/>
</appSettings>
```


