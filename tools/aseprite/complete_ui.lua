return function(art)
local color=art.palette
local skins={panel="f1e6d0",button="e6d7bc",hover="f7edd7",pressed="c8b596",primary="88966a",["primary-hover"]="a1b57c",["primary-pressed"]="63794f",disabled="8c8e83",inset="d6c8af",dark="172b3c",focus="d3a960",track="293b47",fill="ac543f"}
for name,hex in pairs(skins) do
    art.save("ui/"..name,64,64,function()
        art.layer("01 Shadow and clipped corner frame")
        art.poly({{5,0},{58,0},{63,5},{63,58},{58,63},{5,63},{0,58},{0,5}},color.inkSoft)
        art.poly({{6,2},{57,2},{61,6},{61,57},{57,61},{6,61},{2,57},{2,6}},color.wood)
        art.layer("02 Paper or lacquer field")
        art.rect(5,5,54,54,art.color(hex));art.line(7,5,56,5,name=="dark" and color.blue or color.cream)
        art.line(5,7,5,56,name=="dark" and color.blue or color.paper);art.line(7,58,56,58,color.wood)
        art.layer("03 Fine corner motifs and paper fibers")
        for _,corner in ipairs({{7,7},{49,7},{7,49},{49,49}}) do
            local horizontal,vertical=corner[1],corner[2]
            art.line(horizontal,vertical,horizontal+7,vertical,color.blondDark);art.line(horizontal,vertical,horizontal,vertical+7,color.blondDark)
            art.pixel(horizontal+3,vertical+3,color.woodLight)
        end
        if name=="panel" or name=="button" or name=="inset" then for index=0,16 do art.pixel(11+index*17%43,12+index*13%42,art.color("b9a88a",60)) end end
    end)
end
for _,name in ipairs({"thumb","thumb-hover","touch-grip"}) do art.save("ui/"..name,24,24,function()
    art.layer("01 Octagonal fitting");art.poly({{6,1},{17,1},{22,6},{22,17},{17,22},{6,22},{1,17},{1,6}},color.inkSoft);art.poly({{7,3},{16,3},{20,7},{20,16},{16,20},{7,20},{3,16},{3,7}},name=="thumb-hover" and color.cream or color.paper)
    art.layer("02 Engraved grip");art.line(8,8,8,15,color.wood);art.line(12,8,12,15,color.wood);art.line(16,8,16,15,color.wood)
end) end
art.save("ui/touch-disc",64,64,function()
    art.layer("01 Touch pad translucent body");art.ellipse(32,32,30,30,art.color("142638",170))
    art.layer("02 Compass circle and direction notches");art.ring(32,32,28,1,color.paper,1);art.ring(32,32,23,1,color.wood,1)
    for step=0,3 do local phase=step*math.pi/2;art.line(32+math.cos(phase)*22,32+math.sin(phase)*22,32+math.cos(phase)*27,32+math.sin(phase)*27,color.paper,2) end
end)
art.save("ui/arrow",24,24,function()art.layer("01 Arrow outline");art.poly({{4,4},{10,4},{20,12},{10,20},{4,20},{12,12}},color.inkSoft);art.layer("02 Arrow face");art.poly({{7,6},{10,6},{17,12},{10,18},{7,18},{14,12}},color.goldLight)end)
art.save("ui/bookmark",28,44,function()art.layer("01 Vermilion ribbon");art.poly({{3,0},{25,0},{25,42},{14,34},{3,42}},color.redDark);art.poly({{6,1},{22,1},{22,36},{14,30},{6,36}},color.redShade);art.layer("02 Stitched seam");art.line(8,4,8,30,color.redGlint);art.line(20,4,20,30,color.redGlint);art.yinyang(14,15,5)end)
art.save("ui/divider",64,4,function()art.layer("01 Rule");art.rect(0,1,64,1,color.wood);art.layer("02 Center knot");art.rect(29,0,6,3,color.blondDark);art.rect(31,0,2,3,color.paper)end)
art.save("ui/shadow",32,16,function()art.layer("01 Soft ground contact");art.ellipse(16,8,15,6,art.color("111b28",70));art.layer("02 Contact core");art.ellipse(16,8,10,3,art.color("0b1522",90))end)
art.save("ui/stone",24,24,function()art.layer("01 Stone base");art.rect(0,0,24,24,color.stoneDark);art.layer("02 Chipped paver");art.poly({{3,1},{20,1},{23,5},{21,21},{3,23},{1,19},{1,5}},color.stone);art.line(4,2,18,2,color.stoneLight);art.line(20,8,18,13,color.stoneDark)end)
art.save("ui/petal",8,8,function()art.layer("01 Maple petal");art.poly({{1,2},{3,3},{4,0},{5,3},{7,3},{6,5},{4,7},{3,5},{0,5}},color.redShade);art.layer("02 Lit vein");art.line(4,3,4,6,color.redGlint)end)
art.save("ui/panel-spray",120,64,function()art.layer("01 Quiet ink branch");art.line(4,58,103,19,color.wood,2);art.line(41,42,53,8,color.wood);art.layer("02 Maple accents");for index=0,9 do local horizontal=13+index*10;local vertical=51-index*3;art.poly({{horizontal-5,vertical},{horizontal-2,vertical-3},{horizontal,vertical-8},{horizontal+3,vertical-2},{horizontal+8,vertical},{horizontal+2,vertical+4},{horizontal,vertical+8},{horizontal-2,vertical+3}},index%3==0 and color.redShade or color.woodLight)end end)
art.save("ui/shrine_marker",32,48,function()art.layer("01 Stone marker");art.poly({{9,6},{22,6},{25,39},{29,45},{3,45},{7,39}},color.stoneDark);art.poly({{10,8},{20,8},{21,40},{9,40}},color.stoneLight);art.layer("02 Inscribed blessing");art.line(15,12,15,32,color.redDark,2);art.line(11,17,19,17,color.redDark);art.line(11,23,18,21,color.redDark);art.pixel(19,12,color.paper)end)
art.save("ui/night_journal",256,256,function()
    art.layer("01 Ink blue medallion and gilded rim");art.ellipse(128,128,121,121,color.ink);art.ellipse(128,127,117,117,color.wood);art.ellipse(128,125,110,110,color.night);art.ring(128,126,106,1,color.blondDark,2)
    art.layer("02 Crescent moon and crimson shrine gate");art.ellipse(173,67,31,31,color.cream);art.ellipse(184,61,28,28,color.night)
    art.rect(57,100,15,100,color.redDark);art.rect(182,100,15,100,color.redDark);art.rect(59,104,6,91,color.red);art.rect(184,104,6,91,color.red)
    art.poly({{33,77},{63,85},{190,85},{223,74},{218,91},{202,100},{49,100},{37,91}},color.ink);art.rect(46,98,164,10,color.redShade);art.rect(47,132,164,8,color.redShade)
    art.layer("03 Yin-yang seal and starlight");art.yinyang(128,166,39);art.star(54,62,10,color.goldLight);art.star(204,175,7,color.cream);art.star(87,40,4,color.paper)
    art.layer("04 Fallen maple leaves");for index=0,8 do local horizontal=52+index*18;local vertical=204+index%3*7;art.poly({{horizontal-5,vertical},{horizontal,vertical-7},{horizontal+4,vertical-1},{horizontal+9,vertical+1},{horizontal+3,vertical+4},{horizontal,vertical+9},{horizontal-2,vertical+3}},color.redShade)end
end)
end
