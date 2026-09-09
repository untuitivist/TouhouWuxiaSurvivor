return function(root,output)
local board=Image{fromFile=root.."/art/reference/reimu-gameplay-style-corrected.png"}
local pixel=app.pixelColor
local entries={}
local palette={}
for value in ("170f1a 231620 34222c 452731 59323a 70443f 895648 a76b58 ca9772 541421 781427 9d1930 bd2338 dc3645 ef5661 ff827c 8e707f b99da1 d5b8b9 ecd3cc fff1df fffafa db967a f1b18a ffce9e ffe2b9 7b4326 aa6e32 d89540 f7c363 151621 252539 474254 676374 9290a0 cbc3cd f8c5a2 ec704e 7f3a3c"):gmatch("%S+") do
    palette[#palette+1]={tonumber(value:sub(1,2),16),tonumber(value:sub(3,4),16),tonumber(value:sub(5,6),16)}
end
local remapped={}
local function cleanPalette(image)
    for iterator in image:pixels() do
        local value=iterator()
        if pixel.rgbaA(value)>0 then
            if not remapped[value] then
                local red,green,blue=pixel.rgbaR(value),pixel.rgbaG(value),pixel.rgbaB(value)
                local best,bestDistance=nil,math.huge
                for _,candidate in ipairs(palette) do
                    local distance=(red-candidate[1])^2*2+(green-candidate[2])^2*3+(blue-candidate[3])^2
                    if distance<bestDistance then best,bestDistance=candidate,distance end
                end
                remapped[value]=pixel.rgba(best[1],best[2],best[3],255)
            end
            iterator(remapped[value])
        end
    end
end

local function stripBackdrop(image)
    for iterator in image:pixels() do
        local value=iterator()
        local red,green,blue=pixel.rgbaR(value),pixel.rgbaG(value),pixel.rgbaB(value)
        if blue>red+8 and green>red+3 then iterator(0) end
    end
    return image
end

local function isolate(rectangle, width, height, fit)
    local image=stripBackdrop(Image(board,Rectangle(table.unpack(rectangle))))
    local bounds=image:shrinkBounds()
    if bounds.width==0 or bounds.height==0 then error("Empty reference crop") end
    image=Image(image,bounds)
    if fit then
        local ratio=math.min(width/image.width,height/image.height)
        image:resize(math.max(1,math.floor(image.width*ratio+0.5)),math.max(1,math.floor(image.height*ratio+0.5)))
    else image:resize(width,height) end
    return image
end

local function save(name, image, categories, description)
    local source=output.."/source/"..name..".aseprite"
    local texture=output.."/textures/"..name..".png"
    if app.fs.isFile(source) or app.fs.isFile(texture) then error("Refusing to overwrite: "..name) end
    app.fs.makeAllDirectories(app.fs.filePath(source))
    app.fs.makeAllDirectories(app.fs.filePath(texture))
    local sprite=Sprite(image.width,image.height,ColorMode.RGB)
    local original=sprite.layers[1]
    original.name="00 Extracted reference - not a hand redraw"
    sprite:newCel(original,1,Image(image),Point(0,0))
    original.isVisible=false
    original.isEditable=false
    cleanPalette(image)
    for _,category in ipairs(categories) do
        local separated=Image(image.width,image.height,ColorMode.RGB)
        for iterator in image:pixels() do
            if pixel.rgbaA(iterator())>0 and category.accept(iterator.x,iterator.y,iterator()) then separated:drawPixel(iterator.x,iterator.y,iterator()) end
        end
        if not separated:isEmpty() then
            local layer=sprite:newLayer()
            layer.name=category.name
            sprite:newCel(layer,1,separated,Point(0,0))
        end
    end
    sprite:saveAs(source)
    sprite:saveCopyAs(texture)
    entries[#entries+1]={name=name,width=image.width,height=image.height,derivation=description}
    sprite:close()
    print("REFERENCE_ASSET "..name.." "..image.width.."x"..image.height)
end

local function fullLayer(name)
    return {{name=name,accept=function() return true end}}
end

local actor=Image(192,144,ColorMode.RGB)
local columns={704,916,1127,1338}
local rows={505,565,625}
for row,top in ipairs(rows) do
    for column,left in ipairs(columns) do
        local image=isolate({left,top,110,55},44,44,true)
        actor:drawImage(image,Point((column-1)*48+math.floor((48-image.width)/2),(row-1)*48+44-image.height))
    end
end
save("players/reimu",actor,{
    {name="01 Head and hair - cleaned palette",accept=function(horizontal,vertical) return vertical%48<22 end},
    {name="02 Torso and sleeves - cleaned palette",accept=function(horizontal,vertical) return vertical%48>=22 and vertical%48<37 end},
    {name="03 Legs and boots - cleaned palette",accept=function(horizontal,vertical) return vertical%48>=37 end}
},"12 frames from the user-authorized board; backdrop removed, foot alignment and 39-color pixel cleanup; not a full hand redraw")
local preview=Image(actor)
preview:resize(768,576)
preview:saveAs(output.."/reimu-frames-preview.png")

local ofuda=isolate({735,722,79,58},12,23,true)
local talisman=Image(32,32,ColorMode.RGB)
talisman:drawImage(ofuda,Point(math.floor((32-ofuda.width)/2),math.floor((32-ofuda.height)/2)))
save("effects/reimu_talisman",talisman,fullLayer("01 Extracted paper and seal"),"Isolated frontal ofuda from the approved effects board")
local projectile=Image(talisman)
projectile:resize(24,24)
save("combat/ofuda",projectile,fullLayer("01 Pixel-normalized projectile"),"Ofuda derived from the separate talisman; not an orbit sprite")

local orbs=Image(192,48,ColorMode.RGB)
local orbRegions={{726,808,100,63},{928,808,105,63},{1145,808,92,63}}
for frame=1,4 do
    local image=isolate(orbRegions[frame==4 and 2 or frame],38,38,true)
    if frame==4 then image:flip() end
    orbs:drawImage(image,Point((frame-1)*48+math.floor((48-image.width)/2),math.floor((48-image.height)/2)))
end
save("actors/yin_yang_orb",orbs,fullLayer("01 Jade views and retained pixel glints"),"Front, oblique, side, mirrored-oblique views extracted from the board")
local orbPreview=Image(orbs)
orbPreview:resize(768,192)
orbPreview:saveAs(output.."/orb-frames-preview.png")

for _,entry in ipairs({
    {"effects/reimu_seal_ink",{692,890,168,64},128},
    {"effects/reimu_aura",{692,890,168,64},96},
    {"effects/ritual_array",{1125,887,153,69},192}
}) do
    local image=isolate(entry[2],entry[3]-8,entry[3]-8,false)
    local centered=Image(entry[3],entry[3],ColorMode.RGB)
    centered:drawImage(image,Point(4,4))
    save(entry[1],centered,fullLayer("01 Extracted rune plane - unprojected"),"Separate board effect resampled to a world plane; gameplay camera projects it once")
end
local manifest=io.open(output.."/extraction.json","wb")
manifest:write(json.encode({schema=1,editor="Aseprite",reference="art/reference/reimu-gameplay-style-corrected.png",stage="extraction_and_pixel_cleanup_pending_visual_review",assets=entries}))
manifest:write("\n")
manifest:close()
print("REFERENCE_EXTRACTION_PASS "..output)
end
