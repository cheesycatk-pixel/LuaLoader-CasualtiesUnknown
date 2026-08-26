-- know that you'd need a png file in the same folder as the example code, also this wouldn't work as the name needs to be main.lua and not example.lua
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
    info.weight = 0.4
    info.value = math.huge
    info.usable = true
    info.decayMinutes = 180
    info.tags = "cangetwet"
    new_object(Recognition, {2})

    info.useAction = function(body, item)
        body:Eat(12, 0.5)
        body:Drink(4)

        body.happiness = body.happiness + 100
    end

    call_static(ItemRegistry, "Register", {"realfood", info, theSprite})
end
