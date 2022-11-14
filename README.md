# CoradoLog
CoradoLog

Unity Log Extension.

CoLoggerInitializer - setting prefab

Can add senders, context, and importan.
Can enable disable log, from senders and contexts.

Can generate vars for fast access to senders and contexts.
Can generate personale sender debug class.

If you have user Test and generate vars and sender class:
Access after generate:

```csharp
CoLoggerVars.Senders.Test
DebugTest.Log("message");
```

All generated object put to CoradoLogGenerated folder.

Can create log for some context and senders:

```csharp
CoLog log = new CoLog("Test", "TestContext");
log.Log("Test Message");
```
or send directly:

```csharp
Log("Test Message", "TestUser", "TestContext");
```
