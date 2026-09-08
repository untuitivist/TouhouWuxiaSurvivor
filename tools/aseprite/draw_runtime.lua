local root = app.params["root"] or app.fs.currentPath
local painter = dofile(root .. "/tools/aseprite/pixel_tools.lua")
local sourceRoot = root .. "/art/runtime"
local textureRoot = root .. "/assets/aseprite"
app.fs.makeAllDirectories(sourceRoot)
app.fs.makeAllDirectories(textureRoot)
local function color(hex, alpha) return painter.color(hex, alpha) end
local ink, wood, gold = color("382c32"), color("754c38"), color("bb8c4e")
local paper, light = color("f2e3bc"), color("fff1ce")
local function save(name, width, height, draw)
    local sprite = Sprite(width, height, ColorMode.RGB)
    painter.begin(sprite)
    draw()
    painter.finish()
    sprite:saveAs(sourceRoot .. "/" .. name .. ".aseprite")
    sprite:saveCopyAs(textureRoot .. "/" .. name .. ".png")
    sprite:close()
    print("ASEPRITE_ASSET " .. name)
end
local fills = {panel="f2e3bc",button="f2e3bc",primary="bfd0a0",hover="fff1ce",pressed="d7b47d",disabled="d4cab1",inset="e5d3aa",dark="342e2e",track="a68c65",fill="526b51"}
for name, fill in pairs(fills) do
    save(name, 64, 64, function()
        painter.layer("Lacquer silhouette")
        painter.rect(6,8,54,54,ink)
        painter.rect(2,6,58,50,wood)
        painter.rect(6,2,50,58,wood)
        painter.layer("Paper and inset rim")
        painter.rect(6,6,50,50,gold)
        painter.rect(8,8,46,46,color(fill))
        painter.rect(10,8,42,2,name == "dark" and wood or light)
        painter.rect(8,10,2,42,name == "dark" and wood or light)
        painter.rect(10,52,42,2,name == "dark" and wood or color("c8ac7b"))
        painter.layer("Corner joinery")
        if name ~= "fill" and name ~= "track" then
            for _, corner in ipairs({{4,4},{50,4},{4,50},{50,50}}) do
                painter.rect(corner[1],corner[2],8,8,wood)
                painter.rect(corner[1]+2,corner[2]+2,4,4,gold)
                painter.pixel(corner[1]+2,corner[2]+2,light)
            end
        end
        if name == "primary" or name == "pressed" then
            painter.rect(4,16,2,30,color("a84342"))
            painter.rect(56,16,2,30,color("a84342"))
        end
    end)
end
save("focus",64,64,function()
    painter.layer("Cinnabar focus brackets")
    for _, corner in ipairs({{0,0},{52,0},{0,52},{52,52}}) do
        painter.rect(corner[1],corner[2] < 20 and 0 or 60,12,4,color("a84342"))
        painter.rect(corner[1] < 20 and 0 or 60,corner[2],4,12,color("a84342"))
    end
end)
for _, name in ipairs({"thumb","thumb-hover","arrow"}) do
    save(name,24,24,function()
        painter.layer("Control silhouette")
        if name == "arrow" then
            for row=0,3 do painter.rect(4+row*2,8+row*2,16-row*4,2,ink) end
        else
            painter.rect(4,2,16,20,wood)
            painter.layer("Polished grip")
            painter.rect(6,4,12,16,name == "thumb-hover" and light or gold)
            painter.rect(10,8,4,8,paper)
            painter.rect(6,4,12,2,light)
        end
    end)
end
save("stone",24,24,function()
    painter.layer("Mortar")
    painter.rect(0,0,24,24,color("525b5a"))
    painter.layer("Staggered stone slabs")
    for _, slab in ipairs({{1,1,15,10},{18,1,6,10},{0,13,7,10},{9,13,15,10}}) do
        painter.rect(slab[1],slab[2],slab[3],slab[4],color("929a8c"))
        painter.rect(slab[1],slab[2],slab[3]-1,1,color("a4aa97"))
        painter.rect(slab[1],slab[2]+slab[4]-1,slab[3],1,color("757f77"))
    end
    painter.layer("Weathering")
    painter.line(10,1,11,4,color("747f76"))
    painter.line(11,4,9,6,color("747f76"))
    painter.rect(2,9,3,1,color("78896f"))
    painter.rect(19,20,3,1,color("b0b09a"))
end)
save("shrine_marker",32,48,function()
    painter.layer("Stone silhouette")
    painter.poly({{8,4},{23,4},{25,8},{25,36},{29,38},{29,44},{3,44},{3,38},{7,36},{7,8}},ink)
    painter.layer("Carved stone faces")
    painter.rect(9,8,13,28,color("71877d"))
    painter.rect(22,8,2,28,color("415955"))
    painter.rect(10,6,12,2,color("abb4a0"))
    painter.rect(5,38,22,4,color("73877a"))
    painter.rect(5,38,22,1,color("a0ac95"))
    painter.layer("Paper offering and binding")
    painter.rect(11,12,8,17,paper)
    painter.rect(11,12,1,17,color("b7ad8b"))
    painter.rect(14,15,2,3,color("ac5551"))
    painter.rect(13,19,4,1,color("ac5551"))
    painter.rect(15,21,1,4,color("ac5551"))
    painter.line(8,10,23,10,gold)
    painter.rect(7,34,4,2,color("526b51"))
end)
save("petal",8,8,function()
    painter.layer("Petal silhouette")
    painter.poly({{1,3},{3,1},{6,2},{6,4},{4,6},{2,5}},color("c084a1"))
    painter.layer("Moonlit fold")
    painter.line(2,3,4,2,color("efc7cc"))
    painter.line(3,4,5,3,color("dda9be"))
end)
save("shadow",32,16,function()
    painter.layer("Soft stepped shadow")
    painter.ellipse(16,8,15,6,color("111d25",35))
    painter.ellipse(16,8,12,4,color("111d25",65))
end)
save("touch-disc",64,64,function()
    painter.layer("Translucent lacquer")
    painter.ellipse(32,32,30,30,color("101e28",175))
    painter.layer("Ivory ring")
    for row=0,63 do
        for column=0,63 do
            local distance=(column-32)^2+(row-32)^2
            if distance >= 28^2 and distance <= 30^2 then painter.pixel(column,row,color("e8dfc4",210)) end
        end
    end
    painter.layer("Cardinal notches")
    painter.rect(30,3,5,3,gold)
    painter.rect(30,59,5,3,gold)
    painter.rect(3,30,3,5,gold)
    painter.rect(59,30,3,5,gold)
end)
save("touch-grip",24,24,function()
    painter.layer("Grip silhouette")
    painter.ellipse(12,12,10,10,color("3d615d",220))
    painter.layer("Jade face")
    painter.ellipse(12,11,8,8,color("b5d5be",225))
    painter.line(8,6,14,6,color("e3ecd0",230))
end)
save("night_journal",256,256,function()
    painter.layer("Lacquer frame")
    painter.rect(0,0,256,256,color("17272d"))
    painter.rect(8,8,240,240,gold)
    painter.rect(12,12,232,232,wood)
    painter.rect(20,20,216,216,color("192b30"))
    painter.layer("Corner inlays")
    for _, corner in ipairs({{16,16},{216,16},{16,216},{216,216}}) do
        painter.rect(corner[1],corner[2],24,24,wood)
        painter.rect(corner[1]+4,corner[2]+4,16,16,color("a84342"))
        painter.rect(corner[1]+8,corner[2]+8,8,8,gold)
    end
    for _, entry in ipairs({{"orb",32,32,192,192},{"ofuda",22,172,32,48},{"star",194,38,40,40}}) do
        local original = app.open(root .. "/assets/internal_original/base/combat/" .. entry[1] .. ".png")
        local image = original.cels[1].image
        painter.layer("Original TH10 " .. entry[1])
        for row=0,entry[5]-1 do
            for column=0,entry[4]-1 do
                local pixel=image:getPixel(math.floor(column*image.width/entry[4]),math.floor(row*image.height/entry[5]))
                painter.pixel(entry[2]+column,entry[3]+row,pixel)
            end
        end
        original:close()
    end
end)
print("ASEPRITE_RUNTIME_DRAW_PASS")
