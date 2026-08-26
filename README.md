# LuaLoader-CasualtiesUnknown

A LuaLoader for the game 'Casualties: Unknown'

**It's Currently in Early Development and will have constant changes made, Until a stable version is released, expect frequent API and behavior changes**
**Also note that this project might get abandonned and no longer worked on as rather i lost motivation or have other more important projects or just things to do**

Made possible using NLua and KeraLua

## Setup
- Download the .zip file in Releases
- Unzip it inside plugins in the BepInEx Folder
- Do NOT move all the DLLs and the main.lua file outside the folder it got unzipped in
- Open the main.lua file and write your code there, no compilation is required, LuaLoader.dll should execute it once the game launches

## Usage
- Inside the main.lua, you should always start with a global on_scene_loaded() function, this is your main function where everything will be executed
- that's pretty much it, you can use a library like CUCoreLib to ease your programming
- Below is an example of how you can use CUCoreLib:

```lua
function on_scene_loaded(scene)
    local ItemRegistry = get_type("CUCoreLib.Registries.ItemRegistry")
    local ItemInfo = get_type("ItemInfo")
    local Recognition = get_type("Recognition")
    local AssetLoader = get_type("CUCoreLib.Helpers.AssetLoader")

    local theSprite = call_static(AssetLoader, "LoadSpriteFromPluginFolder", {loader, "image.png", 300})

    local info = new_object(ItemInfo, {})
    info.fullName = "realfood"
    info.description = "some desc"
    info.category = "food"
    info.weight = 0.5
    info.value = math.huge
    info.usable = true
    info.decayMinutes = 600
    info.tags = "cangetwet"
    new_object(Recognition, {2})

    info.useAction = function(body, item)
        body:Eat(12, 0.5)
        body:Drink(4)

        body.happiness = body.happiness + 100
    end

    call_static(ItemRegistry, "Register", {"realfood", info, theSprite})
end
```

# API

**Log(message)** - Outputs the message into the LogOutput.log file inside BepInEx's Folder
Params:
- message - The Value to log
```lua
log("Hello!")
```

**get_type(type)** - Finds a loaded C#/.NET type by its full name
- The loader searches through the assemblies currently loaded by the game

Params:
- name (string) - The full name of the C# type

Returns:
- The .NET System.Type object otherwise nil if the type could not be found
```lua
local ItemInfo = get_type("ItemInfo")
local ItemRegistry = get_type("CUCoreLib.Registries.ItemRegistry")
```

**new_object(type, arguments)** - Creates a new instance of a C# type
- The arguments table is used to pass arguments to the C# constructor
Params:
- type - A System.Type, usually returned by get_type()
- arguments (table) - Arguments for the constructor

Returns:
- The newly created C# object
  nil if the object could not be created
```lua
local info = new_object(ItemInfo, {})
local rec = new_object(Recognition, {2})
info.rec = rec
```
(**NOTE**: Arguments are passed to the C# constructor in the same order, same for call_static below)

**call_static(type, method, arguments)** - Calls a static C# method
- The loader finds a static method with the specified name and invokes it using the provided arguments

Params:
- type - A System.Type, usually returned by get_type()
- method (string) - The name of the static C# method
- arguments (table) - Arguments to pass to the method

Returns:
- The return value of the C# method, if it has one
```lua
call_static(
    ItemRegistry,
    "Register",
    {"realfood", info, sprite}
)
```

**find_object(type)** - Finds an existing Unity object of the specified type
- This searches objects currently loaded by Unity and returns the first matching object

Parameters:
- type - A System.Type, usually returned by get_type()

Returns:
- The first matching Unity object
  nil if no object was found

```lua
local Body = get_type("Body")

local body = find_object(Body)
```

**find_objects(type)** - Finds all currently loaded Unity objects of the specified type

Parameters
- type - A System.Type, usually returned by get_type()

Returns:
- a .NET array containing all matching Unity objects

```lua
local sometype = get_type("sometype")

local sometypes = find_objects(sometype)

for i = 0, sometypes.Length - 1 do
    local type = sometypes[i]

    log(type)
end
```
(**NOTE**: .NET arrays use indexes starting at 0, not 1)

**run_after(seconds, functionName)** - Calls a Lua function after a delay
- The function must be globally accessible by its name

Parameters:
- seconds (number) - How many seconds to wait.
- functionName (string) - The name of the Lua function to call

```lua
function hello()
    log("Hello!")
end

run_after(5, "hello")
```

**wait_for_object(typeName, functionName)** - Waits until Unity creates an object of a specified type
- The loader checks every frame until at least one object of that type exists. Once an object is found, the specified Lua function is called with that object

Parameters:
- typeName (string) - The full name of the C# type to search for
- functionName (string) - The name of the Lua function to call when the object is found

```lua
function body_found(body)
    log("Body found: " .. tostring(body))
end

wait_for_object(
    "Body",
    "body_found"
)
```
The callback receives the found Unity object:
```lua
function body_found(body)
    body:RemoveEye()
end
```

**get_enum_values(type)** - Gets all values from a C# enum
Parameters:
- type - A System.Type representing an enum

Returns:
- A .NET array containing all values of the enum
  nil if the provided type is not an enum

```lua
local SomeEnum = get_type("SomeEnum")

local values = get_enum_values(SomeEnum)

for i = 0, values.Length - 1 do
    log(values[i])
end
```

**dump_assembly_types(assemblyName)** - Logs all types inside a loaded .NET assembly
- This is mainly intended as a debugging and exploration tool for finding classes exposed by the game or its libraries

Parameters:
- assemblyName (string) — The name of the assembly

```lua
dump_assembly_types("Assembly-CSharp")
```

### Global objects
**loader** - The loader global refers to the LuaLoader BepInEx plugin instance
- It can be passed to C# methods that expect a BaseUnityPlugin

For example loading an image from the LuaLoader plugin folder:
```lua
local AssetLoader = get_type("CUCoreLib.Helpers.AssetLoader")

local sprite = call_static(
    AssetLoader,
    "LoadSpriteFromPluginFolder",
    {loader, "image.png", 350}
)
```

**gameAssembly** - A reference to the game's Assembly-CSharp .NET assembly
- This is exposed directly to Lua
- It can be useful when interacting with methods that require a System.Reflection.Assembly

**AppDomain** - A reference to 'System.AppDomain.CurrentDomain'
- This gives Lua access to information about assemblies currently loaded in the game's .NET runtime

For example, assemblies can be obtained with:
```lua
local assemblies = AppDomain:GetAssemblies()
```

### Lua callbacks

**on_scene_loaded(scene)** - Called automatically whenever Unity loads a scene
- If your Lua script defines this function, LuaLoader automatically calls it whenever Unity loads a scene

# Accessing C# objects
Objects returned from C# can be interacted with directly through NLua's .NET integration

For example:
```lua
local object = find_object(get_type("sometype"))

object.something = 100
```

C# methods can also be called:
```lua
object:methodcall(100)
```

And C# properties/fields can be read:
```lua
local someValue = object.field
```

# C#/.NET Integration

LuaLoader uses NLua to expose .NET objects to Lua

Types returned by `get_type()` are `System.Type` objects and can be inspected using .NET reflection

```lua
local ItemInfo = get_type("ItemInfo")

log(ItemInfo.FullName)

local methods = ItemInfo:GetMethods()

for i = 0, methods.Length - 1 do
    log(methods[i].Name)
end
```

# QnA
- Q: Why would anyone use this?
- A: tbh idk, i guess compilation isn't required as you can directly change the .lua files, also it's easier(?), for me atleast

- Q: Will this be published to modding 'websites' like Nexus?
- A: Yes, when i will make a stable release which will not have constant API name changes and other, i will publish it there, but for now it's only in early development

# LICENSE
[MIT_License](LICENSE)
