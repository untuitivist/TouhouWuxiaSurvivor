local root=app.params["root"] or app.fs.currentPath
local output=app.params["output"] or root.."/art/characters/reimu/study-02"
for _,name in ipairs({"reimu.aseprite","anatomy.aseprite","comparison.aseprite","reimu.png","anatomy.png","comparison.png","layers.json"}) do
    if app.fs.isFile(output.."/"..name) then error("Refusing to overwrite "..output.."/"..name..". Choose a new output directory; manual edits must be preserved.") end
end
app.fs.makeAllDirectories(output)
local art=dofile(root.."/tools/aseprite/reimu_anatomy_brush.lua")
local drawBody=dofile(root.."/tools/aseprite/reimu_anatomy_base.lua")
local costume=dofile(root.."/tools/aseprite/reimu_anatomy_costume.lua")
local portrait=dofile(root.."/tools/aseprite/reimu_anatomy_portrait.lua")
local function flatten(sprite)
    local image=Image(sprite.spec)
    image:drawSprite(sprite,1)
    return image
end
local anatomy=Sprite(192,288,ColorMode.RGB)
art.begin(anatomy)
drawBody(art,true)
art.finish()
local anatomyImage=flatten(anatomy)
anatomy:saveAs(output.."/anatomy.aseprite")
anatomy:saveCopyAs(output.."/anatomy.png")
anatomy:close()
local sprite=Sprite(192,288,ColorMode.RGB)
art.begin(sprite)
costume.back(art)
drawBody(art,false)
costume.clothes(art)
portrait(art)
art.finish()
local figure=flatten(sprite)
local overlay=sprite:newLayer()
overlay.name="00 Construction overlay - toggle visibility to inspect"
overlay.opacity=125
sprite:newCel(overlay,1,anatomyImage,Point(0,0))
overlay.isVisible=false
local manifest=io.open(output.."/layers.json","wb")
manifest:write('{"schema":1,"editor":"Aseprite","status":"study_pending_visual_review","source":"reimu.aseprite","width":192,"height":288,"layers":[')
for index,layer in ipairs(sprite.layers) do
    if index>1 then manifest:write(",") end
    manifest:write(string.format('{"name":"%s","visible":%s,"opacity":%d}',layer.name,tostring(layer.isVisible),layer.opacity))
end
manifest:write(']}\n');manifest:close()
sprite:saveAs(output.."/reimu.aseprite")
sprite:saveCopyAs(output.."/reimu.png")
sprite:close()
local board=Sprite(1256,672,ColorMode.RGB)
art.begin(board)
art.layer("Review board background")
art.rect(0,0,1256,672,art.painter.color("252e42"))
local text=dofile(root.."/tools/aseprite/study_text.lua")(art.painter)
for index,label in ipairs({"01 / STRUCTURE","02 / SHAPE CHECK","03 / COLOR STUDY"}) do
    local left=12+(index-1)*418
    art.rect(left,54,398,593,art.painter.color("34404b"))
    art.line(left,53,left+397,53,"goldShade")
    text(label,left+18,23,2,art.colors.cream)
end
local function paste(image,left,top,solid)
    for row=0,image.height-1 do for column=0,image.width-1 do
        local pixel=image:getPixel(column,row)
        if app.pixelColor.rgbaA(pixel)>0 then art.rect(left+column*2,top+row*2,2,2,solid or pixel) end
    end end
end
art.layer("Construction view")
paste(anatomyImage,19,63)
art.layer("Costume silhouette view")
paste(figure,437,63,art.colors.creamShade)
art.layer("Painted figure view")
paste(figure,855,63)
art.layer("Scope label")
text("REIMU / ANATOMY 02 - ASEPRITE LAYERED SOURCE - NOT RUNTIME ART",25,658,1,art.colors.creamShade)
art.finish()
board:saveAs(output.."/comparison.aseprite")
board:saveCopyAs(output.."/comparison.png")
board:close()
print("REIMU_ANATOMY_STUDY_PASS "..output)
