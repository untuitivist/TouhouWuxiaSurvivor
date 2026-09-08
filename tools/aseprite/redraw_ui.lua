return function(art)
local palette=art.palette
local ink,wood,gold=art.color("382c32"),art.color("754c38"),art.color("bb8c4e")
local paper,light=art.color("f2e3bc"),art.color("fff1ce")
for name,fill in pairs({panel="f2e3bc",inset="e5d3aa",button="f2e3bc",hover="fff1ce",pressed="d7b47d",disabled="d4cab1",primary="bfd0a0",["primary-hover"]="d5e0b2",["primary-pressed"]="a7b781",dark="342e2e",track="a68c65",fill="526b51"}) do
    art.save("ui/"..name,64,64,function()
        art.layer("Approved brown timber silhouette")
        art.rect(6,8,54,54,ink)
        art.rect(2,6,58,50,wood)
        art.rect(6,2,50,58,wood)
        art.layer("Warm paper and inset rim")
        art.rect(6,6,50,50,gold)
        art.rect(8,8,46,46,art.color(fill))
        art.rect(10,8,42,2,name=="dark" and wood or light)
        art.rect(8,10,2,42,name=="dark" and wood or light)
        art.rect(10,52,42,2,name=="dark" and wood or art.color("c8ac7b"))
        art.layer("Square corner joinery")
        if name~="fill" and name~="track" then
            for _,corner in ipairs({{4,4},{50,4},{4,50},{50,50}}) do
                art.rect(corner[1],corner[2],8,8,wood)
                art.rect(corner[1]+2,corner[2]+2,4,4,gold)
                art.pixel(corner[1]+2,corner[2]+2,light)
            end
        end
        if name:sub(1,7)=="primary" or name=="pressed" then
            art.rect(4,16,2,30,art.color("a84342"))
            art.rect(56,16,2,30,art.color("a84342"))
        end
    end)
end
art.save("ui/focus",64,64,function()
    art.layer("Vermilion focus marks")
    for _,corner in ipairs({{1,1,1,1},{62,1,-1,1},{1,62,1,-1},{62,62,-1,-1}}) do
        local horizontal,vertical,signX,signY=table.unpack(corner)
        art.line(horizontal,vertical,horizontal+10*signX,vertical,palette.red,2)
        art.line(horizontal,vertical,horizontal,vertical+10*signY,palette.red,2)
    end
end)
for _,name in ipairs({"thumb","thumb-hover","touch-grip"}) do
    art.save("ui/"..name,24,24,function()
        art.layer("Jade and brass grip")
        art.poly({{12,1},{22,11},{22,14},{12,23},{2,14},{2,11}},palette.ink)
        art.poly({{12,3},{20,11},{20,13},{12,21},{4,13},{4,11}},palette.gold)
        art.poly({{12,6},{17,11},{17,13},{12,18},{7,13},{7,11}},name=="thumb-hover" and palette.mint or palette.jade)
        art.layer("Edge light");art.line(10,9,12,7,palette.white,2)
    end)
end
art.save("ui/arrow",24,24,function()art.layer("Folded arrow");art.poly({{4,7},{20,7},{12,17}},palette.ink);art.poly({{7,8},{17,8},{12,14}},palette.goldShade);art.line(7,8,17,8,palette.gold)end)
art.save("ui/bookmark",28,44,function()art.layer("Ribbon");art.poly({{3,1},{25,1},{25,42},{14,36},{3,42}},palette.ink);art.poly({{5,2},{23,2},{23,38},{14,33},{5,38}},palette.redShade);art.rect(7,3,13,26,palette.red);art.layer("Seal medallion");art.star(14,15,6,palette.gold);art.rect(13,12,2,6,palette.paper)end)
art.save("ui/divider",64,4,function()art.layer("Woven divider");art.line(0,1,63,1,palette.goldShade);for column=3,63,8 do art.rect(column,0,2,3,palette.gold)end end)
art.save("ui/panel-spray",120,64,function()
    art.layer("Quiet plum branch");local stem=art.color("9d7046",32);local bloom=art.color("bd5357",28)
    art.line(120,57,37,27,stem,2);art.line(92,47,76,11,stem);art.line(68,37,27,44,stem)
    for _,point in ipairs({{78,15},{40,28},{29,43},{97,49},{64,35}})do art.cluster(point[1],point[2],5,4,bloom);art.pixel(point[1],point[2],art.color("d7ab65",85))end
end)
art.save("ui/stone",24,24,function()
    art.layer("Night stone tile");art.rect(0,0,24,24,palette.ink);art.rect(1,1,22,22,palette.blue)
    art.rect(2,2,20,18,palette.slate);art.line(3,2,19,2,palette.mist);art.line(2,3,2,17,palette.mist)
    art.layer("Stone cracks and moss");art.line(14,9,17,13,palette.blue);art.line(17,13,21,14,palette.blue);art.rect(3,19,4,2,palette.jadeShade);art.pixel(7,18,palette.jade)
end)
art.save("ui/shrine_marker",32,48,function()
    art.layer("Carved old seal stone");art.poly({{8,8},{13,3},{24,7},{26,38},{31,44},{1,44},{6,37}},palette.ink)
    art.poly({{10,9},{14,5},{22,9},{23,36},{8,36}},palette.slate);art.line(10,10,10,32,palette.mist)
    art.rect(6,37,21,4,palette.blue);art.rect(4,41,25,2,palette.mist)
    art.layer("Vermilion inscription");art.ring(16,19,6,1,palette.gold);art.line(16,14,16,25,palette.paper);art.line(12,18,20,18,palette.paper);art.rect(14,27,4,2,palette.red)
end)
art.save("ui/petal",8,8,function()art.layer("Falling petal");art.poly({{1,4},{3,1},{6,1},{7,4},{4,7}},palette.red);art.poly({{2,4},{4,2},{6,2},{5,5},{3,6}},palette.rose);art.pixel(4,3,palette.paper)end)
art.save("ui/shadow",32,16,function()art.layer("Soft stepped shadow");art.ellipse(16,8,15,6,art.color("172631",55));art.ellipse(16,8,11,4,art.color("172631",95))end)
art.save("ui/touch-disc",64,64,function()art.layer("Movement ring");art.ellipse(32,32,29,29,art.color("172631",70));art.ring(32,32,29,1,art.color("d7ab65",130));art.ring(32,32,25,1,art.color("efe3c3",85));art.layer("Compass points");for index=0,3 do local angle=index*math.pi/2;art.star(32+math.cos(angle)*23,32+math.sin(angle)*23,3,palette.gold)end end)
art.save("ui/night_journal",256,256,function()
    art.layer("Night lacquer emblem");art.rect(28,12,200,232,palette.ink);art.rect(12,28,232,200,palette.ink);art.rect(28,20,200,216,palette.goldShade);art.rect(20,28,216,200,palette.goldShade);art.rect(30,26,196,204,palette.deep);art.rect(26,30,204,196,palette.deep)
    art.layer("Balance medallion");art.orb(128,128,77,0);art.ring(128,128,86,1,palette.gold,2)
    art.layer("Paper talisman and starlight");art.talisman(55,151,4);art.star(199,65,28,palette.ink);art.star(199,65,23,palette.gold);art.star(199,62,15,palette.yellow);art.star(196,59,7,palette.white)
    art.line(40,31,89,31,palette.paper,2);art.line(31,40,31,86,palette.paper,2);art.line(171,224,215,224,palette.paper,2);art.line(224,172,224,213,palette.paper,2)
end)
end
