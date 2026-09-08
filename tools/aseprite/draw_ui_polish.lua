local root = app.params["root"] or app.fs.currentPath
local painter = dofile(root .. "/tools/aseprite/pixel_tools.lua")
local function color(hex, alpha) return painter.color(hex, alpha) end
local ink, brass, edge = color("283c3b"), color("b39664"), color("dfc99a")
local function save(name, width, height, draw)
    local sprite = Sprite(width, height, ColorMode.RGB)
    painter.begin(sprite)
    draw()
    painter.finish()
    sprite:saveAs(root .. "/art/runtime/" .. name .. ".aseprite")
    sprite:saveCopyAs(root .. "/assets/aseprite/" .. name .. ".png")
    sprite:close()
    print("POLISHED_UI " .. name)
end
local fills = {panel="e8e3cd",inset="f3edd9",button="dddcc8",hover="fff5d9",pressed="cccbb7",disabled="c6c7b9",primary="345b53",["primary-hover"]="426f61",["primary-pressed"]="27473f",dark="172c30",track="63746a",fill="94b9a0"}
for name, fill in pairs(fills) do
    save(name,64,64,function()
        local primary = name:sub(1,7) == "primary"
        local dark = name == "dark" or primary
        local panel = name == "panel"
        painter.layer("Cut lacquer and cast shadow")
        painter.rect(5,7,56,55,color("0d1a20",155))
        painter.rect(3,5,56,54,ink)
        painter.rect(5,3,52,58,ink)
        painter.layer("Brass inlay and inner bevel")
        painter.rect(6,6,50,50,brass)
        painter.rect(7,7,48,48,dark and color("51665a") or color("afa988"))
        painter.rect(9,9,44,44,color(fill))
        painter.line(10,9,51,9,dark and color("6f8871") or color("fff5df"))
        painter.line(9,10,9,51,dark and color("597662") or color("fff5df"))
        painter.line(10,52,52,52,dark and color("122b2c") or color("b7b79e"))
        painter.layer("Paper fibres and lacquer pores")
        for row=14,49 do
            for column=14,49 do
                local hash=(column*73+row*97+column*row*17)%157
                if hash < 4 then painter.pixel(column,row,dark and color("f1e8c9",7) or color("6c6650",9)) end
                if hash == 20 then painter.pixel(column,row,dark and color("101c24",12) or color("ffffff",34)) end
            end
        end
        painter.layer("Corner brasswork")
        if name ~= "fill" and name ~= "track" then
            for _, corner in ipairs({{4,4,1,1},{57,4,-1,1},{4,57,1,-1},{57,57,-1,-1}}) do
                for offset=0,7 do
                    painter.pixel(corner[1]+offset*corner[3],corner[2],edge)
                    painter.pixel(corner[1],corner[2]+offset*corner[4],edge)
                end
                painter.rect(corner[1]+corner[3]*3,corner[2]+corner[4]*3,2,2,brass)
            end
        end
        if panel then
            painter.rect(3,15,2,34,color("38554e"))
            painter.rect(57,15,2,34,color("38554e"))
            for row=16,48,8 do painter.rect(3,row,2,2,brass); painter.rect(57,row,2,2,brass) end
        end
        if primary then
            painter.rect(7,25,2,12,color("d1b97f"))
            painter.rect(53,25,2,12,color("d1b97f"))
        end
    end)
end
save("focus",64,64,function()
    painter.layer("Gold focus corners")
    for _, corner in ipairs({{0,0,1,1},{63,0,-1,1},{0,63,1,-1},{63,63,-1,-1}}) do
        for offset=0,9 do
            painter.pixel(corner[1]+offset*corner[3],corner[2],color("a64c48"))
            painter.pixel(corner[1],corner[2]+offset*corner[4],color("a64c48"))
        end
        painter.pixel(corner[1]+corner[3]*2,corner[2]+corner[4]*2,color("f8e7b9"))
    end
end)
for _, name in ipairs({"thumb","thumb-hover"}) do
    save(name,24,24,function()
        painter.layer("Brass slider grip")
        painter.poly({{12,1},{21,10},{21,14},{12,23},{3,14},{3,10}},ink)
        painter.poly({{12,3},{19,11},{19,13},{12,21},{5,13},{5,11}},brass)
        painter.layer("Jade inset")
        painter.poly({{12,6},{17,11},{17,13},{12,18},{7,13},{7,11}},color(name == "thumb-hover" and "cee1bd" or "83ab94"))
        painter.line(11,8,9,11,edge)
    end)
end
save("bookmark",28,44,function()
    painter.layer("Folded silk ribbon")
    painter.poly({{1,0},{27,0},{27,43},{14,35},{1,43}},color("7d3338"))
    painter.poly({{3,1},{24,1},{24,38},{14,32},{3,38}},color("ab534f"))
    painter.layer("Gold stitching")
    painter.line(4,2,4,33,color("d8a177"))
    painter.line(23,2,23,33,color("d8a177"))
    painter.line(7,29,20,29,color("d8a177"))
end)
save("divider",64,4,function()
    painter.layer("Stitched header rule")
    painter.rect(0,1,64,1,color("b5ad87"))
    painter.rect(0,2,64,1,color("f7f0d8"))
    painter.rect(7,0,2,3,color("aaa27b"))
    painter.rect(39,0,2,3,color("aaa27b"))
end)
save("panel-spray",120,64,function()
    painter.layer("Plum branches")
    local branch=color("776857")
    painter.line(118,61,76,43,branch,2)
    painter.line(76,43,43,21,branch,2)
    painter.line(43,21,13,13,branch)
    painter.line(93,50,87,25,branch)
    painter.line(87,25,72,9,branch)
    painter.line(58,31,45,43,branch)
    painter.line(45,43,22,48,branch)
    painter.layer("Muted jade leaves")
    for _, leaf in ipairs({{19,14},{33,45},{67,35},{88,29},{102,53}}) do
        painter.poly({{leaf[1],leaf[2]},{leaf[1]-7,leaf[2]-7},{leaf[1]-10,leaf[2]-6},{leaf[1]-6,leaf[2]-1}},color("718b78"))
    end
    painter.layer("Five petal blossoms")
    for _, flower in ipairs({{16,12},{43,23},{74,10},{88,27},{26,48},{61,33}}) do
        local centerX,centerY=flower[1],flower[2]
        for index=0,4 do
            local angle=index*math.pi*2/5-math.pi/2
            painter.ellipse(math.floor(centerX+math.cos(angle)*4),math.floor(centerY+math.sin(angle)*4),3,3,color("c2827c"))
        end
        painter.ellipse(centerX,centerY,2,2,color("ead2a3"))
        painter.pixel(centerX,centerY,color("ac775e"))
    end
end)
print("ASEPRITE_UI_POLISH_PASS")
