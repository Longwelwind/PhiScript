# PhiScript
PhiScript is a mod loader and a library for developping

## Projects
### PhiPatcher
PhiPatcher is an executable that alters the bytecode of Planetbase to tell the game to load PhiScript
(which will load mods) and to emit events at specific moments.

In order to make sure that the dependencies for PhiPatcher are working correctly you are going to have to initialize the sub-modules and update them. You can do this by running the following commands inside your local copy.

```
git submodule init
git submodule update
```

### PhiScript
PhiScript loasd mods and offers them helpers methods to add content to the game (Ressources, Objects, Modules, ...)

### PhiScriptExample and PermissionLandingPadButton
These are 2 examples of mod.
