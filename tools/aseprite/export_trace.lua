local root=app.params["root"] or app.fs.currentPath
local source=app.params["source"] or root.."/art/trace/reimu_front_02.aseprite"
local output=app.params["output"] or root.."/artifacts/aseprite-tracing/export"
app.fs.makeAllDirectories(output)
local sprite=app.open(source)
if not sprite then error("Cannot open source: "..source) end
local layers={}
local painted=0
for _,layer in ipairs(sprite.layers) do
    local reference=layer.name:find("REFERENCE ONLY",1,true)~=nil
    if reference then layer.isVisible=false end
    local cel=layer:cel(1)
    local nonempty=cel and not cel.image:isEmpty() or false
    if layer.isVisible and not reference and nonempty then painted=painted+1 end
    layers[#layers+1]={name=layer.name,visible=layer.isVisible,editable=layer.isEditable,nonempty=nonempty,reference=reference}
end
if painted<5 then error("Expected independently editable artwork layers") end
sprite:saveCopyAs(output.."/reimu-front.png")
local preview=Image{fromFile=output.."/reimu-front.png"}
preview:resize(sprite.width*4,sprite.height*4)
preview:saveAs(output.."/reimu-front-4x.png")
local file=assert(io.open(output.."/report.json","wb"))
file:write(json.encode({source=source,width=sprite.width,height=sprite.height,painted_layers=painted,reference_excluded=true,layers=layers,stage="rejected_front_study_not_runtime"}))
file:write("\n")
file:close()
sprite:close()
print("ASEPRITE_TRACE_EXPORT_PASS painted_layers="..painted)
