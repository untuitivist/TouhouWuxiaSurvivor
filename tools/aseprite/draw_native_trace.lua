local root=app.params["root"] or app.fs.currentPath
local edition=app.params["edition"] or "native-v01"
if not edition:match("^[a-z0-9-]+$") then error("ASCII edition required") end
local sourceRoot=root.."/art/trace/"..edition
local output=root.."/artifacts/"..edition
local sourceMarker=sourceRoot.."/manifest.json"
if app.fs.isFile(sourceMarker) then error("Refusing to overwrite completed sources") end
app.fs.makeAllDirectories(sourceRoot)
app.fs.makeAllDirectories(output)
local log=assert(io.open(output.."/trace.log","ab"))
local function report(message)
    print(message)
    log:write(message.."\n")
    log:flush()
end
local engine=dofile(root.."/tools/aseprite/native_trace_engine.lua")
local makeLayout=dofile(root.."/tools/aseprite/native_trace_layouts.lua")
local manifest={schema=1,editor="Aseprite",method="automated_native_pencil_reference_retracing",manual_hand_painting=false,reference_colors_preserved=true,resized=false,palette_quantized=false,runtime_eligible=false,assets={}}
local function drawAsset(hero,name,reference,rectangle,layerFor,transparent,referencePath)
    local path=sourceRoot.."/"..hero.."/"..name..".aseprite"
    if app.fs.isFile(path) then error("Refusing to overwrite editable source: "..path) end
    app.fs.makeAllDirectories(app.fs.filePath(path))
    app.fs.makeAllDirectories(output.."/"..hero)
    local sprite=Sprite(rectangle.width,rectangle.height,ColorMode.RGB)
    report("TRACE_START "..hero.."/"..name)
    local result=engine.paint(sprite,reference,rectangle,layerFor,transparent,function(message) report(hero.."/"..name.." "..message) end)
    local verification=engine.verify(sprite,reference,rectangle,layerFor,transparent)
    if verification.color_mismatches~=0 or verification.unexpected_pixels~=0 then error("Native pencil retracing mismatch: "..hero.."/"..name) end
    local layerNames={}
    for _,layer in ipairs(sprite.layers) do layerNames[#layerNames+1]=layer.name end
    local guide=sprite:newLayer()
    guide.name="REFERENCE ONLY - original guide, hidden and locked"
    sprite:newCel(guide,1,Image(reference,rectangle),Point(0,0))
    guide.isVisible=false
    guide.isEditable=false
    sprite:saveAs(path)
    sprite:saveCopyAs(output.."/"..hero.."/"..name..".png")
    manifest.assets[#manifest.assets+1]={name=hero.."/"..name,source=path:sub(#root+2),reference=referencePath,reference_rectangle={rectangle.x,rectangle.y,rectangle.width,rectangle.height},width=rectangle.width,height=rectangle.height,transparent=transparent,strokes=result.strokes,painted_pixels=result.painted_pixels,layers=layerNames,verification=verification,method=manifest.method,manual_hand_painting=false}
    sprite:close()
    report("TRACE_ASSET_PASS "..hero.."/"..name.." strokes="..result.strokes.." matched="..verification.matching_pixels)
end
for _,hero in ipairs({"reimu","marisa"}) do
    local referencePath=hero=="reimu" and "art/reference/reimu-gameplay-style-corrected.png" or "art/reference/marisa-gameplay-style-approved.png"
    local reference=Image{fromFile=root.."/"..referencePath}
    local layout=makeLayout(hero,engine)
    drawAsset(hero,"character_board",reference,Rectangle(0,0,reference.width,reference.height),layout.boardLayer,false,referencePath)
    drawAsset(hero,"portrait",reference,layout.portrait,function(horizontal,vertical,value)
        if layout.figure(horizontal,vertical,value) then return layout.portraitPart(horizontal,vertical,value) end
    end,true,referencePath)
    for row,top in ipairs(layout.spriteRows) do
        for column,left in ipairs(layout.spriteColumns) do
            local rectangle=Rectangle(left,top,112,60)
            local function part(horizontal,vertical,value)
                local localX,localY=horizontal-left,vertical-top
                if localY<16 then return "01 Headwear and hair ribbon" end
                if localY<35 then return "02 Head hair and face" end
                if localY>=50 then return "06 Lower legs and boots" end
                if localX<49 then return "03 Left sleeve and hand" end
                if localX>72 then return "04 Right sleeve and hand" end
                return "05 Tunic scarf and belt"
            end
            drawAsset(hero,string.format("walk_%02d_%02d",row,column),reference,rectangle,part,true,referencePath)
        end
    end
end
local file=assert(io.open(sourceMarker,"wb"))
file:write(json.encode(manifest))
file:write("\n")
file:close()
report("NATIVE_TRACE_PACK_PASS assets="..#manifest.assets.." source="..sourceRoot)
log:close()
