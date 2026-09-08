local root=app.params["root"] or app.fs.currentPath
local art=dofile(root.."/tools/aseprite/redraw_brush.lua")
for _,name in ipairs({"actors","effects","scenery","ui","title"}) do dofile(root.."/tools/aseprite/redraw_"..name..".lua")(art) end
local manifest=io.open(root.."/art/redraw/manifest.json","wb")
table.sort(art.entries,function(left,right)return left.name<right.name end)
manifest:write('{"schema":1,"editor":"Aseprite","provenance":"Drawn from blank canvases; reference image pixels are never imported","assets":[')
for index,entry in ipairs(art.entries) do
    if index>1 then manifest:write(",") end
    manifest:write(string.format('{"name":"%s","width":%d,"height":%d}',entry.name,entry.width,entry.height))
end
manifest:write("]}\n");manifest:close()
print("ASEPRITE_FULL_REDRAW_PASS "..#art.entries)
