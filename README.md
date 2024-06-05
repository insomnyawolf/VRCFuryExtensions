# VRCFuryExtensions

Just some tools that i wish they existed

For this to work you need to add the following internals override

I will need to find a way to automate that process at some point

```cs
// VRCFury, VRCFury.Player
[assembly: InternalsVisibleTo("VRCFuryExtensions")]
[assembly: InternalsVisibleTo("VRCFuryExtensionsEditor")]

// VRCFury-Editor
[assembly: InternalsVisibleTo("VRCFuryExtensionsEditor")]
```

