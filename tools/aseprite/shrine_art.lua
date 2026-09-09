local root=app.params["root"] or app.fs.currentPath
local output=app.params["output"] or root.."/artifacts/shrine-pack-01"
local normalized=output:gsub("\\","/")
local base=root:gsub("\\","/")
if normalized:find("..",1,true) or not (normalized:sub(1,10)=="artifacts/" or normalized:sub(1,#base+11)==base.."/artifacts/") then
    error("Extraction studies are reference-only and may only be saved below artifacts/")
end
local pixel=app.pixelColor
local art={root=root,output=output,entries={},images={}}
local colors={}
for value in ("170f1a 231620 34222c 452731 59323a 70443f 895648 a76b58 ca9772 541421 781427 9d1930 bd2338 dc3645 ef5661 ff827c 8e707f b99da1 d5b8b9 ecd3cc fff1df fffafa db967a f1b18a ffce9e ffe2b9 7b4326 aa6e32 d89540 f7c363 151621 252539 474254 676374 9290a0 cbc3cd f8c5a2 ec704e 7f3a3c 1c2938 263b4c 3c5062 56657a 73829a 91a9bb c8d5d7 28352e 3c4c3d 556348 738257 96a36c c0c891 dce0b5 494a45 626259 7b796b 969180 b3ad98 d2c5aa 382d2b 5f4333 8b6447 b68b62 e6bb7e e8d59f 592d36 985046 d07852 b77637 d4a347 e9c36b"):gmatch("%S+") do
    colors[#colors+1]={tonumber(value:sub(1,2),16),tonumber(value:sub(3,4),16),tonumber(value:sub(5,6),16)}
end
local cache={}
function art.clean(image)
    for iterator in image:pixels() do
        local value=iterator()
        if pixel.rgbaA(value)>0 then
            if not cache[value] then
                local red,green,blue=pixel.rgbaR(value),pixel.rgbaG(value),pixel.rgbaB(value)
                local best,bestDistance=nil,math.huge
                for _,candidate in ipairs(colors) do
                    local distance=(red-candidate[1])^2*2+(green-candidate[2])^2*3+(blue-candidate[3])^2
                    if distance<bestDistance then best,bestDistance=candidate,distance end
                end
                cache[value]=pixel.rgba(best[1],best[2],best[3],255)
            end
            iterator(cache[value])
        end
    end
    return image
end
function art.key(image)
    for iterator in image:pixels() do
        local value=iterator()
        local red,green,blue=pixel.rgbaR(value),pixel.rgbaG(value),pixel.rgbaB(value)
        if green>120 and green>red*1.55+20 and green>blue*1.55+20 then iterator(0) end
    end
    return image
end
function art.crop(image,rectangle,width,height,key)
    local result=Image(image,Rectangle(table.unpack(rectangle)))
    if key then
        art.key(result)
        local bounds=result:shrinkBounds()
        if bounds.width==0 or bounds.height==0 then error("Empty chroma crop") end
        result=Image(result,bounds)
    end
    local ratio=math.min(width/result.width,height/result.height)
    result:resize(math.max(1,math.floor(result.width*ratio+0.5)),math.max(1,math.floor(result.height*ratio+0.5)))
    return result
end
local function paths(name)
    local source=output.."/source/"..name..".aseprite"
    local texture=output.."/textures/"..name..".png"
    if app.fs.isFile(source) or app.fs.isFile(texture) then error("Refusing to overwrite editable art: "..name) end
    app.fs.makeAllDirectories(app.fs.filePath(source))
    app.fs.makeAllDirectories(app.fs.filePath(texture))
    return source,texture
end
function art.finish(name,sprite,entry)
    local source,texture=paths(name)
    sprite:saveAs(source)
    sprite:saveCopyAs(texture)
    entry.name=name
    entry.width=sprite.width
    entry.height=sprite.height
    art.entries[#art.entries+1]=entry
    art.images[name]=Image{fromFile=texture}
    sprite:close()
    print("SHRINE_ASSET "..name.." "..entry.width.."x"..entry.height)
end
function art.save(name,image,entry,regions)
    local sprite=Sprite(image.width,image.height,ColorMode.RGB)
    local reference=sprite.layers[1]
    reference.name="00 Extracted reference - not a hand redraw"
    sprite:newCel(reference,1,Image(image),Point(0,0))
    reference.isVisible=false
    reference.isEditable=false
    art.clean(image)
    regions=regions or {{name="01 Cleaned pixels",accept=function() return true end}}
    for _,region in ipairs(regions) do
        local layer=sprite:newLayer()
        layer.name=region.name
        local separated=Image(image.width,image.height,ColorMode.RGB)
        for iterator in image:pixels() do
            if pixel.rgbaA(iterator())>0 and region.accept(iterator.x,iterator.y,iterator()) then separated:drawPixel(iterator.x,iterator.y,iterator()) end
        end
        sprite:newCel(layer,1,separated,Point(0,0))
    end
    art.finish(name,sprite,entry)
end
function art.inherit(name,source,entry)
    local sprite=app.open(source)
    if not sprite then error("Cannot open Aseprite source: "..source) end
    art.finish(name,sprite,entry)
end
function art.actor(name,image,regions,frameHeight,targetHeight)
    local strip=Image(192,frameHeight*#regions,ColorMode.RGB)
    for row,region in ipairs(regions) do
        for column=0,3 do
            local cell=art.crop(image,{column*image.width/4,region[1],image.width/4,region[2]},44,targetHeight,true)
            strip:drawImage(cell,Point(column*48+math.floor((48-cell.width)/2),(row-1)*frameHeight+44-cell.height))
        end
    end
    art.save(name,strip,{columns=4,rows=#regions,frame_width=48,frame_height=48,foot_anchor=44,process="generated_reference_extraction_pixel_cleanup",reference=name=="players/marisa" and "art/reference/marisa-walk-generated-01.png" or "art/reference/enemies-walk-generated-01.png"},{
        {name="01 Head hair and headwear",accept=function(horizontal,vertical) return vertical%48<24 end},
        {name="02 Body clothes and hands",accept=function(horizontal,vertical) return vertical%48>=24 and vertical%48<37 end},
        {name="03 Feet and lower hem",accept=function(horizontal,vertical) return vertical%48>=37 end}
    })
end
return art
