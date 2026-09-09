local root=app.params["root"] or app.fs.currentPath
local edition=app.params["edition"] or "03b"
if not edition:match("^[a-z0-9]+$") then error("ASCII edition required") end
local source=root.."/art/trace/reimu_front_"..edition..".aseprite"
local output=root.."/artifacts/aseprite-tracing/reimu-front-"..edition
local sprite=assert(app.open(source),"Cannot reopen study source")
assert(sprite.width==112 and sprite.height==60 and #sprite.frames==1,"Unexpected source dimensions or frames")
local layers={}
local guides={}
local visibleCount=0
for _,layer in ipairs(sprite.layers) do
    local isGuide=layer.name:match("^GUIDE %-") or layer.name:match("^REFERENCE %-")
    local pixels=0
    for _,cel in ipairs(sprite.cels) do
        if cel.layer==layer then
            for iterator in cel.image:pixels() do
                if app.pixelColor.rgbaA(iterator())>0 then pixels=pixels+1 end
            end
        end
    end
    assert(pixels>0,"Empty layer: "..layer.name)
    if isGuide then
        assert(not layer.isVisible and not layer.isEditable,"Guide must stay hidden and locked: "..layer.name)
        guides[#guides+1]=layer
    else
        assert(layer.isVisible and layer.isEditable,"Drawing layer must be visible and editable: "..layer.name)
        visibleCount=visibleCount+1
    end
    layers[#layers+1]={name=layer.name,pixels=pixels,visible=layer.isVisible,editable=layer.isEditable}
end
assert(#guides==2,"Expected the anatomy and reference guides")
assert(visibleCount>=10,"Expected separate editable body-part layers")
sprite:saveCopyAs(output.."/reopened.png")
for _,layer in ipairs(guides) do sprite:deleteLayer(layer) end
sprite:saveCopyAs(output.."/without-guides.png")
sprite:close()
local file=assert(io.open(output.."/source-audit.json","wb"))
file:write(json.encode({source=source:sub(#root+2),drawing_layers=visibleCount,guide_layers=2,layers=layers,
    source_saved_over=false,visual_approval=false,scope="Editable source and guide exclusion only"}).."\n")
file:close()
print("FRONT_SOURCE_AUDIT drawing_layers="..visibleCount.." guides=2 source_saved_over=false visual_approval=false")
