local root=app.params["root"] or app.fs.currentPath
local art=dofile(root.."/tools/aseprite/shrine_art.lua")
local extraction=art.output.."/extraction"
dofile(root.."/tools/aseprite/extract_gameplay_reference.lua")(root,extraction)
local pixel=app.pixelColor
local function readJson(path)
    local file=assert(io.open(path,"rb"))
    local text=file:read("*a")
    file:close()
    return json.decode(text)
end
local replacements={}
for _,name in ipairs({"players/reimu","players/marisa","actors/kedama","actors/wild_fairy","actors/mountain_spirit","actors/great_youkai","actors/yin_yang_orb","effects/reimu_talisman","combat/ofuda","effects/reimu_seal_ink","effects/reimu_aura","effects/ritual_array","scenery/title_shrine","scenery/torii","scenery/tree_canopy_a","scenery/tree_canopy_b"}) do replacements[name]=true end
for _,entry in ipairs(readJson(root.."/art/redraw/manifest.json").assets) do
    if not replacements[entry.name] then
        art.inherit(entry.name,root.."/art/redraw/"..entry.name..".aseprite",{process="retained_layered_aseprite",reference="art/redraw/"..entry.name..".aseprite"})
    end
end
for _,name in ipairs({"players/reimu","actors/yin_yang_orb","effects/reimu_talisman","combat/ofuda","effects/reimu_seal_ink","effects/reimu_aura","effects/ritual_array"}) do
    local entry={process="user_board_extraction_pixel_cleanup",reference="art/reference/reimu-gameplay-style-corrected.png"}
    if name=="players/reimu" or name=="actors/yin_yang_orb" then
        entry.columns=4;entry.rows=name=="players/reimu" and 3 or 1
        entry.frame_width=48;entry.frame_height=48;entry.foot_anchor=name=="players/reimu" and 44 or 24
    end
    art.inherit(name,extraction.."/source/"..name..".aseprite",entry)
end
local marisa=Image{fromFile=root.."/art/reference/marisa-walk-generated-01.png"}
art.actor("players/marisa",marisa,{{0,362},{362,362},{724,362}},48,44)
local enemies=Image{fromFile=root.."/art/reference/enemies-walk-generated-01.png"}
for _,entry in ipairs({{"actors/kedama",{40,230},30},{"actors/wild_fairy",{280,270},44},{"actors/mountain_spirit",{550,310},44},{"actors/great_youkai",{860,360},44}}) do
    art.actor(entry[1],enemies,{entry[2]},48,entry[3])
end
local map=Image{fromFile=root.."/art/reference/shrine-courtyard-generated-02.png"}
local title=Image(map,Rectangle(0,80,1536,864))
title:resize(640,360)
art.save("scenery/title_shrine",title,{process="generated_map_crop_pixel_cleanup",reference="art/reference/shrine-courtyard-generated-02.png"},{
    {name="01 Skyline shrine and woodland",accept=function(horizontal,vertical) return vertical<130 end},
    {name="02 Courtyard and approach",accept=function(horizontal,vertical) return vertical>=130 end}
})
local tiles=Image(256,64,ColorMode.RGB)
for index,rectangle in ipairs({{540,390,192,138},{745,390,192,138},{1050,360,192,138},{325,367,192,138}}) do
    local tile=Image(map,Rectangle(table.unpack(rectangle)))
    tile:resize(64,64)
    for iterator in tile:pixels() do
        local horizontal,vertical=iterator.x,iterator.y
        if horizontal>31 then horizontal=63-horizontal end
        if vertical>31 then vertical=63-vertical end
        iterator(tile:getPixel(horizontal,vertical))
    end
    tiles:drawImage(tile,Point((index-1)*64,0))
end
art.save("scenery/courtyard_tiles",tiles,{process="generated_map_tile_extraction_seam_cleanup",reference="art/reference/shrine-courtyard-generated-02.png",columns=4,rows=1,frame_width=64,frame_height=64})
dofile(root.."/tools/aseprite/shrine_props.lua")(art)
dofile(root.."/tools/aseprite/shrine_atlas.lua")(art)
table.sort(art.entries,function(left,right) return left.name<right.name end)
local manifest=assert(io.open(art.output.."/manifest.json","wb"))
manifest:write(json.encode({schema=2,editor="Aseprite",stage="reference_study_only",runtime_eligible=false,provenance="Extraction and cleanup study only. The user requires Aseprite tracing/redrawing; these pixels must not be promoted to runtime artwork.",assets=art.entries}))
manifest:write("\n")
manifest:close()
print("SHRINE_PACK_PASS "..#art.entries)
