local root=app.params["root"] or app.fs.currentPath
local source=root.."/art/characters/reimu/study-02"
local output=root.."/artifacts/reimu-anatomy-verification"
app.fs.makeAllDirectories(output)
for _,name in ipairs({"reimu","anatomy","comparison"}) do
    local sprite=app.open(source.."/"..name..".aseprite")
    assert(sprite,"Cannot reopen "..name)
    assert(#sprite.frames==1,"Study must contain one pose")
    if name~="comparison" then assert(sprite.width==192 and sprite.height==288) end
    sprite:saveCopyAs(output.."/"..name..".png")
    if name=="reimu" then
        assert(#sprite.layers>=20,"Expected separately editable character parts")
        local guide
        for _,layer in ipairs(sprite.layers) do
            assert(layer.isEditable,"Character layer must be editable: "..layer.name)
            if layer.name=="00 Construction overlay - toggle visibility to inspect" then guide=layer end
        end
        assert(guide and not guide.isVisible,"Construction overlay must default to hidden")
        assert(guide.opacity==125,"Construction overlay should be translucent")
        guide.isVisible=true
        sprite:saveCopyAs(output.."/guide-visible.png")
        print("REIMU_EDITABLE_LAYERS_PASS "..#sprite.layers)
    end
    sprite:close()
end
print("REIMU_SOURCE_REOPEN_PASS")
