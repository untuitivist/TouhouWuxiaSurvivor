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
local function lantern(horizontal,vertical)
    art.rect(horizontal-2,vertical,4,25,palette.ink)
    art.rect(horizontal-6,vertical+2,12,11,palette.goldShade)
    art.rect(horizontal-4,vertical+3,8,8,palette.yellow)
    art.rect(horizontal-2,vertical+3,3,8,palette.white)
    art.line(horizontal-9,vertical+1,horizontal+9,vertical+1,palette.ink,2)
    art.rect(horizontal-6,vertical+13,12,2,palette.ink)
end
art.save("scenery/title_shrine",640,360,function()
    art.layer("Moonlit sky and distant ridges")
    art.rect(0,0,640,360,palette.ink)
    for index,hex in ipairs({"243743","2a414e","344f5b","48616b","5c7278"}) do art.rect(0,35+index*27,640,30,art.color(hex)) end
    art.ellipse(463,76,36,36,palette.mist);art.ellipse(466,73,33,33,palette.paper);art.ellipse(470,68,26,26,palette.white)
    art.cluster(445,80,8,6,palette.paper);art.cluster(480,91,6,4,palette.gold)
    for _,point in ipairs({{56,20},{97,48},{253,39},{381,22},{553,45},{607,19},{340,75}}) do art.pixel(point[1],point[2],palette.paper) end
    art.poly({{0,180},{42,154},{83,171},{126,137},{176,160},{233,126},{289,155},{348,116},{410,161},{475,131},{542,153},{602,123},{640,145},{640,300},{0,300}},palette.blue)
    art.poly({{0,204},{83,182},{141,197},{193,174},{248,206},{329,177},{378,199},{459,169},{523,191},{596,169},{640,198},{640,360},{0,360}},palette.deep)
    art.layer("Shrine courtyard and receding steps")
    for _,tree in ipairs({{57,150,54},{114,169,34},{247,142,57},{532,140,61},{595,148,58},{22,181,45}}) do
        for tier=0,5 do local top=tree[2]+tier*tree[3]/8;local half=5+tier*3;art.poly({{tree[1],top},{tree[1]-half,top+17},{tree[1]+half,top+17}},tier%2==0 and palette.jadeShade or palette.deep) end
    end
    for row=228,359,7 do for column=0,639,9 do
        if column<380-(row-210)*1.7 or column>410+(row-210)*1.7 then
            local hash=(column*29+row*43)%31
            if hash<6 then art.line(column,row,column+4,row-2,palette.jadeShade) end
            if hash==7 then art.line(column,row,column+2,row-3,palette.jade) end
        end
    end end
    art.poly({{346,213},{436,213},{632,360},{119,360}},palette.slate)
    for row=231,359,14 do
        local expand=(row-210)*1.7
        art.line(386-expand,row,405+expand,row,palette.ink,3)
        art.line(386-expand,row+3,405+expand,row+3,palette.mist)
        for column=0,5 do art.rect(386-expand+column*expand/3,row+4,1,9,palette.blue) end
        for column=0,7 do
            local left=386-expand+column*expand/4
            art.line(left+7,row+6,left+13,row+7,palette.blue)
            if column%3==0 then art.line(left+4,row+10,left+11,row+10,palette.jadeShade) end
        end
    end
    art.layer("Shrine architecture")
    art.rect(365,149,99,73,palette.ink);art.rect(372,161,85,57,palette.redShade)
    art.rect(382,164,65,52,palette.goldShade);art.rect(387,168,56,46,palette.deep)
    art.rect(390,171,50,29,palette.paper)
    for column=391,440,7 do art.rect(column,171,2,41,palette.hair) end
    art.rect(387,185,56,2,palette.hair);art.rect(387,199,56,2,palette.hair)
    art.poly({{341,158},{365,148},{392,126},{433,126},{464,148},{483,154},{471,164},{353,165}},palette.ink)
    art.poly({{351,157},{376,147},{395,130},{431,130},{454,148},{474,155},{465,159},{358,160}},palette.blue)
    for row=137,156,6 do art.line(393-(row-131)*1.4,row,432+(row-131)*1.4,row,palette.slate) end
    art.line(391,128,433,128,palette.gold,2);art.rect(359,216,112,5,palette.ink);art.rect(365,215,100,2,palette.goldShade)
    art.layer("Vermilion gate and courtyard lanterns")
    torii(285,124,1.75)
    lantern(290,242);lantern(493,242);lantern(237,293);lantern(548,293)
    art.layer("Foreground cherry branches")
    art.poly({{0,0},{17,0},{27,65},{69,98},{119,110},{117,116},{62,108},{20,83},{11,151},{0,174}},palette.hair)
    art.line(6,5,23,73,palette.hairLight,4)
    for _,group in ipairs({{12,25,62,22},{62,14,56,25},{98,38,51,24},{47,75,40,20},{134,10,47,23},{624,112,53,36},{597,157,34,26}}) do
        art.cluster(group[1],group[2],group[3],group[4],palette.redShade)
        art.cluster(group[1]-5,group[2]-7,group[3]-8,group[4]-6,palette.red)
        art.cluster(group[1]-12,group[2]-10,group[3]-20,math.max(3,group[4]-12),palette.rose)
        for row=-group[4]+3,group[4]-3,5 do for column=-group[3]+5,group[3]-5,6 do
            if column*column/group[3]^2+row*row/group[4]^2<0.83 then
                local hash=(column*7+row*11)%13
                if hash<5 then art.cluster(group[1]+column,group[2]+row,3,2,hash<2 and palette.paper or palette.rose) end
                if hash==7 then art.line(group[1]+column,group[2]+row,group[1]+column+4,group[2]+row,palette.redShade) end
            end
        end end
    end
    art.layer("Petals and distant fireflies")
    for _,point in ipairs({{87,107},{159,79},{228,126},{284,91},{509,139},{561,201},{583,300},{121,273}}) do art.line(point[1],point[2],point[1]+2,point[2]-1,palette.rose,2) end
    for _,point in ipairs({{84,216},{173,233},{558,245},{606,210},{309,272}}) do art.pixel(point[1],point[2],palette.gold) end
end)
end
