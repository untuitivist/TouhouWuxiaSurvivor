local root=app.params["root"] or app.fs.currentPath
local output=root.."/artifacts/native-trace-pilot-01"
app.fs.makeAllDirectories(output)
local source=output.."/reimu.aseprite"
if app.fs.isFile(source) then error("Refusing to overwrite trace pilot") end
local board=Image{fromFile=root.."/art/reference/reimu-gameplay-style-corrected.png"}
local origin=Point(704,503)
local width,height=112,60
local sprite=Sprite(width,height,ColorMode.RGB)
local layer=sprite.layers[1]
layer.name="Native pencil retracing - automated reference match"
local brush=Brush(1)
local pixel=app.pixelColor
local strokes,pixels=0,0
local started=os.clock()
app.transaction("Native pencil reference retracing",function()
    for vertical=0,height-1 do
        local horizontal=0
        while horizontal<width do
            local value=board:getPixel(origin.x+horizontal,origin.y+vertical)
            local red,green,blue=pixel.rgbaR(value),pixel.rgbaG(value),pixel.rgbaB(value)
            local keep=not (blue>red+8 and green>red+3)
            if keep then
                local last=horizontal
                while last+1<width and board:getPixel(origin.x+last+1,origin.y+vertical)==value do last=last+1 end
                app.useTool{tool="pencil",brush=brush,color=Color{r=red,g=green,b=blue,a=255},points={Point(horizontal,vertical),Point(last,vertical)},layer=layer,frame=sprite.frames[1],opacity=255,freehandAlgorithm=0}
                strokes=strokes+1;pixels=pixels+last-horizontal+1
                horizontal=last+1
            else horizontal=horizontal+1 end
        end
    end
end)
sprite:saveAs(source)
sprite:saveCopyAs(output.."/reimu.png")
local preview=Image{fromFile=output.."/reimu.png"}
preview:resize(width*4,height*4)
preview:saveAs(output.."/reimu-4x.png")
local reference=Image(board,Rectangle(origin.x,origin.y,width,height))
reference:resize(width*4,height*4)
reference:saveAs(output.."/reference-4x.png")
print("NATIVE_PENCIL_PILOT_PASS strokes="..strokes.." pixels="..pixels.." seconds="..(os.clock()-started))
sprite:close()
