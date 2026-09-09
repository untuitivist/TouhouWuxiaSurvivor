local root=app.params["root"] or app.fs.currentPath
local art=dofile(root.."/tools/aseprite/complete_brush.lua")
if app.fs.isFile(art.sourceRoot.."/manifest.json") then
    print("COMPLETE_ART_SKIPPED existing_edition="..art.edition)
    return
end
for _,name in ipairs({"characters","portraits","enemies","effects","scenery","ui","atlas","boards"}) do
    dofile(root.."/tools/aseprite/complete_"..name..".lua")(art)
end
table.sort(art.entries,function(left,right)return left.name<right.name end)
local file=assert(io.open(art.sourceRoot.."/manifest.json","wb"))
file:write(json.encode({schema=3,editor="Aseprite",edition=art.edition,manual_mouse_painting=false,reference_pixels_in_export=false,
    runtime_eligible=true,visual_approval="not_recorded",palette=art.hex,assets=art.entries}).."\n")
file:close()
print("ASEPRITE_COMPLETE_PACK_CREATED assets="..#art.entries.." source="..art.sourceRoot)
