return function(art)
local palette=art.palette
local function torii(horizontal,vertical,scale)
    local function rectangle(left,top,width,height,color) art.rect(horizontal+left*scale,vertical+top*scale,width*scale,height*scale,color) end
    local function polygon(points,color) local shifted={};for _,point in ipairs(points) do shifted[#shifted+1]={horizontal+point[1]*scale,vertical+point[2]*scale} end;art.poly(shifted,color) end
    rectangle(17,12,7,82,palette.ink);rectangle(70,12,7,82,palette.ink)
    rectangle(18,15,5,72,palette.redShade);rectangle(71,15,5,72,palette.redShade)
    rectangle(18,16,2,69,palette.red);rectangle(71,16,2,69,palette.red)
    rectangle(16,85,9,9,palette.slate);rectangle(69,85,9,9,palette.slate)
    rectangle(16,85,9,2,palette.mist);rectangle(69,85,9,2,palette.mist)
    polygon({{1,6},{13,9},{79,9},{92,5},{91,12},{80,16},{12,16},{2,12}},palette.ink)
    polygon({{3,8},{15,11},{78,11},{89,8},{88,11},{79,14},{14,14},{4,11}},palette.red)
    rectangle(10,24,74,6,palette.ink);rectangle(11,24,72,3,palette.red)
    rectangle(41,13,10,16,palette.goldShade);rectangle(42,14,8,14,palette.deep)
    rectangle(45,17,2,7,palette.gold);rectangle(44,18,4,1,palette.paper)
    for index=0,5 do
        local left=26+index*7
        polygon({{left,34},{left+2,35},{left+1,39},{left+3,41},{left+1,44},{left-1,41},{left,38}},palette.paper)
    end
    polygon({{22,32},{35,35},{57,35},{73,32},{72,34},{57,37},{35,37},{22,34}},palette.goldShade)
end
art.torii=torii
art.save("scenery/torii",224,256,function()art.layer("Shrine gate timber and stone");torii(0,10,2.35);art.layer("Moonlit gate edge");art.line(15,28,39,34,palette.gold,2)end)
for _,name in ipairs({"tree_canopy_a","tree_canopy_b"}) do
    art.save("scenery/"..name,256,256,function()
        local cherry=name=="tree_canopy_b"
        art.layer("Branch silhouette")
        art.poly({{108,230},{119,149},{65,112},{75,102},{128,135},{172,81},{181,91},{141,151},{149,229},{164,245},{94,245}},palette.ink)
        art.poly({{115,229},{128,156},{132,144},{138,226},{147,239}},palette.hair)
        art.line(120,220,131,165,palette.hairLight,3)
        art.layer("Interlocking leaf masses")
        local groups={{74,105,53,45},{129,64,55,47},{181,104,52,43},{108,143,60,43},{166,148,52,36},{57,147,36,31}}
        for index,group in ipairs(groups) do
            local centerX,centerY,width,height=table.unpack(group)
            art.cluster(centerX,centerY,width,height,palette.ink)
            art.cluster(centerX,centerY-4,width-3,height-4,cherry and palette.redShade or palette.jadeShade)
            art.cluster(centerX-6,centerY-10,width-10,height-10,cherry and palette.red or palette.jade)
            art.cluster(centerX-12,centerY-16,width-22,height-22,cherry and palette.rose or palette.mist)
            for row=-height+8,height-8,7 do for column=-width+8,width-8,8 do
                if column*column/(width*width)+row*row/(height*height)<0.75 then
                    local hash=(column*13+row*17+index*31)%9
                    if hash<3 then art.line(centerX+column,centerY+row,centerX+column+4,centerY+row-1,cherry and palette.rose or palette.jade,2) end
                    if hash==5 then art.rect(centerX+column,centerY+row,3,2,cherry and palette.paper or palette.mint) end
                end
            end end
        end
        art.layer("Scattered leaf edge accents")
        for _,point in ipairs({{39,126},{75,37},{156,27},{213,132},{186,182},{70,185}}) do art.cluster(point[1],point[2],4,3,cherry and palette.rose or palette.mint) end
    end)
end
end
