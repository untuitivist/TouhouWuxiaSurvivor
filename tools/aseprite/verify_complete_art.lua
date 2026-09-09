local root=app.params["root"] or app.fs.currentPath
local edition=assert(app.params["edition"],"edition required")
local output=assert(app.params["output"],"output required")
local manifestFile=assert(io.open(root.."/art/"..edition.."/manifest.json","rb"))
local manifest=json.decode(manifestFile:read("*a"));manifestFile:close()
app.fs.makeAllDirectories(output)
local reports={}
for _,entry in ipairs(manifest.assets) do
    local sprite=assert(app.open(root.."/"..entry.source),entry.source)
    assert(sprite.width==entry.source_width and sprite.height==entry.source_height,"Source dimensions: "..entry.name)
    assert(#sprite.frames==entry.frames,"Timeline frames: "..entry.name)
    local layers={}
    for _,layer in ipairs(sprite.layers) do
        assert(layer.isVisible or layer.name:find("REFERENCE ONLY",1,true),"Unexpected hidden artwork: "..entry.name)
        if layer.name:find("REFERENCE ONLY",1,true) then assert(not layer.isVisible and not layer.isEditable,"Reference must remain hidden and locked") end
        layers[#layers+1]={name=layer.name,visible=layer.isVisible,editable=layer.isEditable}
    end
    local tags={}
    for _,tag in ipairs(sprite.tags) do tags[#tags+1]={name=tag.name,first=tag.fromFrame.frameNumber,last=tag.toFrame.frameNumber} end
    local sheet=Image(entry.width,entry.height,ColorMode.RGB)
    if entry.frames>1 then
        for frame=1,#sprite.frames do
            local image=Image(sprite.width,sprite.height,ColorMode.RGB)
            image:drawSprite(sprite,frame)
            sheet:drawImage(image,Point(((frame-1)%entry.columns)*sprite.width,math.floor((frame-1)/entry.columns)*sprite.height))
        end
    else sheet:drawSprite(sprite,1) end
    sheet:saveAs(output.."/"..entry.name:gsub("/","-")..".png")
    reports[#reports+1]={name=entry.name,layers=layers,tags=tags,frames=#sprite.frames}
    sprite:close()
end
local reportFile=assert(io.open(output.."/aseprite-reopened.json","wb"))
reportFile:write(json.encode(reports).."\n");reportFile:close()
print("ASEPRITE_REOPEN_PASS assets="..#reports)
