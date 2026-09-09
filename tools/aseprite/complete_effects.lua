return function(art)
local color=art.palette
local function talisman(size)
    local horizontal=math.floor(size/2)
    local half=math.max(3,math.floor(size*0.17))
    art.layer("01 Paper silhouette and warm shaded fold")
    art.poly({{horizontal-half-1,2},{horizontal+half+1,2},{horizontal+half,size-3},{horizontal,size-1},{horizontal-half,size-3}},color.redDark)
    art.rect(horizontal-half,3,half*2,size-7,color.paper)
    art.rect(horizontal-half+1,4,half-1,size-9,color.white)
    art.layer("02 Vermilion border and written strokes")
    art.line(horizontal-half+1,4,horizontal+half-2,4,color.red)
    art.line(horizontal-half+1,size-6,horizontal+half-2,size-6,color.red)
    for row=6,size-8,4 do art.line(horizontal-1,row,horizontal+1,row+2,color.red);art.pixel(horizontal+1,row,color.red) end
end
art.save("combat/ofuda",16,16,function()talisman(16)end)
art.save("effects/reimu_talisman",32,32,function()talisman(32)end)
for _,name in ipairs({"star","stardust"}) do
    art.save("combat/"..name,16,16,function()
        art.layer("01 Faint warm aura");art.star(8,8,8,art.color(name=="star" and "d89540" or "8f6fc9",120))
        art.layer("02 Gold star contour and facets");art.star(8,8,7,name=="star" and color.gold or color.lavender)
        art.star(8,8,5,name=="star" and color.goldLight or color.cyanLight)
        art.poly({{8,3},{9,7},{12,8},{9,9},{8,12},{7,9},{5,8},{7,7}},color.white)
        art.line(5,6,8,4,color.cream)
    end)
end
for _,name in ipairs({"red_pellet","violet_pellet"}) do
    art.save("combat/"..name,16,16,function()
        art.layer("01 Bullet dark rim");art.ellipse(8,8,7,7,color.ink)
        art.layer("02 Colored light and white core");art.ellipse(8,8,6,6,name=="red_pellet" and color.redShade or color.violet)
        art.ellipse(7,7,4,4,name=="red_pellet" and color.redGlint or color.lavender)
        art.ellipse(6,6,2,2,color.white)
    end)
end
for _,name in ipairs({"dream","orb"}) do art.save("combat/"..name,32,32,function()art.layer("01 Taiji orb");art.yinyang(16,16,11);art.layer("02 Glowing perimeter");art.ring(16,16,14,1,color.paper,1);art.star(7,7,3,color.white)end) end
art.save("combat/experience",16,16,function()
    art.layer("01 Spirit mote outline");art.poly({{8,1},{14,6},{13,12},{8,15},{2,11},{2,5}},color.jade)
    art.layer("02 Crystal facets");art.poly({{8,3},{12,6},{8,12},{4,10},{4,6}},color.mint);art.line(8,4,7,9,color.white,2)
end)
art.save("combat/healing",16,16,function()
    art.layer("01 Red healing charm");art.poly({{4,2},{12,2},{14,6},{13,12},{8,15},{3,12},{2,6}},color.redDark)
    art.poly({{5,3},{11,3},{12,7},{11,11},{8,13},{4,10}},color.red)
    art.layer("02 White blessing knot");art.rect(7,4,2,7,color.paper);art.rect(5,7,6,2,color.paper);art.pixel(4,4,color.redGlint)
end)
local function array(size,kind)
    local middle=size/2;local radius=size*0.43
    art.layer("01 Amber underglow");art.ring(middle,middle,radius,1,art.color("da634b",70),5)
    art.layer("02 Vermilion and gold concentric inscriptions")
    art.ring(middle,middle,radius,1,color.red,2);art.ring(middle,middle,radius-4,1,color.goldLight,1)
    art.ring(middle,middle,radius*0.76,1,color.redShade,2);art.ring(middle,middle,radius*0.55,1,color.gold,1)
    for step=0,7 do
        local angle=step*math.pi/4;local tangentX,tangentY=-math.sin(angle),math.cos(angle)
        for bar=0,2 do
            local distance=radius*0.83+bar*3
            local horizontal,vertical=middle+math.cos(angle)*distance,middle+math.sin(angle)*distance
            art.line(horizontal-tangentX*4,vertical-tangentY*4,horizontal+tangentX*4,vertical+tangentY*4,color.paper)
        end
        local horizontal,vertical=middle+math.cos(angle)*radius*0.65,middle+math.sin(angle)*radius*0.65
        art.line(middle,middle,horizontal,vertical,color.redShade)
        art.star(middle+math.cos(angle)*radius,middle+math.sin(angle)*radius,3,color.white)
    end
    art.layer("03 Central taiji and cardinal seals");art.yinyang(middle,middle,size*0.1)
    if kind=="square" then
        art.line(15,15,size-16,15,color.red,2);art.line(size-16,15,size-16,size-16,color.red,2)
        art.line(size-16,size-16,15,size-16,color.red,2);art.line(15,size-16,15,15,color.red,2)
    end
end
art.save("effects/reimu_seal_ink",128,128,function()array(128,"square")end)
art.save("effects/reimu_seal",128,128,function()array(128,"round")end)
art.save("effects/ritual_array",256,256,function()array(256,"round")end)
art.save("effects/reimu_aura",96,96,function()
    art.layer("01 Soft perimeter light");art.ring(48,48,42,1,art.color("ff9b80",80),4)
    art.layer("02 Orbit marks");art.ring(48,48,38,1,color.gold,1)
    for step=0,11 do local phase=step*math.pi/6;art.star(48+math.cos(phase)*40,48+math.sin(phase)*40,3,color.paper) end
end)
art.save("effects/marisa_cast",64,64,function()
    art.layer("01 Hakkero charge aura");art.ring(32,32,27,1,art.color("8370d0",100),4);art.ring(32,32,23,1,color.lavender,2)
    art.layer("02 Eight-point furnace pattern")
    for step=0,7 do local phase=step*math.pi/4;art.line(32+math.cos(phase)*14,32+math.sin(phase)*14,32+math.cos(phase)*23,32+math.sin(phase)*23,color.goldLight,2) end
    art.star(32,32,17,color.gold);art.star(32,32,13,color.cream);art.star(13,16,4,color.white)
end)
art.save("effects/master_spark",256,128,function()
    art.layer("01 Violet edge and blue plasma mantle")
    art.poly({{0,46},{15,35},{34,31},{52,19},{87,26},{115,17},{148,21},{182,15},{215,25},{255,21},{255,103},{217,110},{184,105},{150,113},{118,106},{87,111},{52,103},{26,93},{10,82},{0,78}},art.color("8b67df",120))
    art.poly({{0,49},{20,37},{48,33},{67,28},{99,33},{131,26},{169,31},{207,27},{255,30},{255,98},{216,101},{181,96},{149,104},{113,98},{80,100},{47,92},{20,87},{0,74}},color.violet)
    art.layer("02 Cyan current and luminous core")
    art.poly({{0,52},{25,43},{52,39},{81,41},{110,35},{141,38},{180,34},{219,39},{255,34},{255,94},{222,90},{188,94},{149,89},{117,92},{81,87},{45,88},{19,78},{0,72}},color.cyan)
    art.poly({{0,57},{25,49},{59,48},{92,51},{124,45},{156,49},{190,45},{223,49},{255,43},{255,83},{221,78},{189,84},{153,79},{124,83},{90,77},{55,80},{23,73},{0,67}},color.cyanLight)
    art.poly({{0,61},{37,55},{70,58},{109,54},{143,58},{180,53},{222,56},{255,51},{255,75},{211,71},{176,75},{141,70},{104,75},{70,68},{37,72},{0,66}},color.white)
    art.layer("03 Stellar spray and sharp motion streaks")
    for step=0,11 do
        local horizontal=16+step*21;local vertical=step%2==0 and 29 or 100
        art.star(horizontal,vertical,step%3+3,color.lavender);art.pixel(horizontal,vertical,color.white)
        art.line(horizontal,44+step%3*6,horizontal+17,43+step%3*6,color.white)
    end
end)
end
