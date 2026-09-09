local root = app.params["root"] or "."
for _, character in ipairs({"reimu", "marisa"}) do
    local board = Image{fromFile=root .. "/ai-assets/" .. character .. "_pixel_concept.png"}
    local exported = Image{fromFile=root .. "/assets/ui/portraits/ai-preview/" .. character .. ".png"}
    local sprite = app.open(root .. "/art/portraits/ai-preview/" .. character .. ".aseprite")
    assert(sprite.width == 520 and sprite.height == 800 and #sprite.frames == 1, "Portrait source size/frame contract")
    assert(#sprite.layers == 2, "Keep exactly the reference and editable foreground layers")
    local reference, foreground
    for _, layer in ipairs(sprite.layers) do
        if layer.name:find("Original AI", 1, true) then reference = layer end
        if layer.name:find("AI portrait extract", 1, true) then foreground = layer end
    end
    assert(reference and not reference.isVisible and not reference.isEditable, "Original crop remains hidden and locked")
    assert(foreground and foreground.isVisible and foreground.isEditable, "Foreground remains editable")
    local rendered = Image(sprite.spec)
    rendered:drawSprite(sprite, 1)
    local opaque = 0
    for iterator in rendered:pixels() do
        local value = iterator()
        assert(value == exported:getPixel(iterator.x, iterator.y), "PNG must equal the visible Aseprite source")
        if app.pixelColor.rgbaA(value) > 0 then
            opaque = opaque + 1
            assert(value == board:getPixel(iterator.x + 16, iterator.y + 140), "Visible pixels must retain the original AI image colors")
        end
    end
    assert(opaque > 50000 and opaque < 300000, "Portrait is neither empty nor an unmasked board")
    assert(app.pixelColor.rgbaA(exported:getPixel(0, 0)) == 0, "Corner remains transparent")
    print("AI_PORTRAIT_SOURCE_PASS " .. character .. " visible_pixels=" .. opaque)
    sprite:close()
end
