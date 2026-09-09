return function(art)
local color=art.palette
for _,kind in ipairs({"kedama","wild_fairy","mountain_spirit","great_youkai"}) do
    art.animation("actors/"..kind,48,48,4,1,function(frame)
        local hop=frame%2
        art.layer("01 Ground shadow");art.ellipse(24,41,15,4,art.color("111a27",110))
        art.offset(0,hop)
        art.layer("02 Silhouette and moving appendages")
        if kind=="kedama" then
            art.poly({{12,12},{18,13},{22,8},{28,12},{34,10},{35,17},{41,21},{37,26},{39,32},{33,32},{29,39},{24,35},{17,38},{16,32},{9,31},{11,25},{6,20},{12,18}},color.ink)
            art.cluster(24,24,13,13,color.deep);art.cluster(22,21,10,8,color.blue)
            art.poly({{13,22},{18,15},{21,16},{17,23}},color.slate)
        elseif kind=="wild_fairy" then
            local wing=frame%2==0 and 3 or -2
            art.poly({{21,22},{9,12+wing},{3,13+wing},{7,25},{17,32},{11,35},{17,39},{24,30},{32,40},{38,35},{33,30},{43,24},{45,13+wing},{37,12+wing},{27,22}},color.ink)
            art.poly({{18,23},{9,16+wing},{7,17+wing},{11,25},{18,29}},color.clothDark)
            art.poly({{30,23},{38,16+wing},{41,17+wing},{37,25},{30,29}},color.clothShade)
            art.poly({{19,10},{29,10},{33,16},{30,22},{34,29},{33,39},{25,35},{18,40},{14,33},{17,24},{15,18}},color.ink)
            art.poly({{20,12},{28,12},{30,17},{27,23},{30,28},{29,35},{24,31},{20,36},{18,31},{20,23},{18,18}},color.deep)
            art.line(22,13,26,13,color.blue,2)
        elseif kind=="mountain_spirit" then
            art.poly({{11,5},{19,10},{26,9},{36,3},{34,16},{39,22},{36,31},{40,37},{32,40},{26,35},{20,40},{12,39},{13,31},{8,24},{12,17}},color.ink)
            art.poly({{14,11},{18,14},{28,12},{32,10},{31,19},{35,23},{33,31},{28,35},{22,33},{17,36},{17,28},{12,24},{16,20}},color.deep)
            art.poly({{14,8},{17,13},{15,17},{13,14}},color.clothShade)
            art.poly({{31,10},{34,7},{32,17},{30,17}},color.paper)
            art.line(17,33,21,35,color.redDark,2)
        else
            art.poly({{7,5},{15,12},{20,10},{28,9},{34,12},{42,4},{40,21},{45,27},{41,36},{38,43},{27,39},{20,44},{10,41},{8,33},{3,27},{9,19}},color.ink)
            art.poly({{13,13},{20,13},{27,12},{34,15},{36,21},{41,27},{36,36},{34,39},{27,36},{20,40},{13,37},{12,30},{8,27},{14,21}},color.deep)
            art.poly({{13,16},{21,14},{27,14},{33,17},{30,27},{25,31},{18,27}},color.clothDark)
            art.poly({{16,17},{22,16},{28,17},{28,23},{24,27},{19,25}},color.stoneLight)
            art.line(9,9,13,19,color.wood,3);art.line(38,10,34,20,color.woodLight,3)
            art.line(16,31,31,33,color.redDark,3)
        end
        art.layer("03 Face glow and readable eye cores")
        local eyeY=kind=="wild_fairy" and 18 or kind=="great_youkai" and 21 or 23
        art.ellipse(19,eyeY,3,3,color.redDark);art.ellipse(28,eyeY,3,3,color.redDark)
        art.rect(18,eyeY-1,2,3,color.red);art.rect(27,eyeY-1,2,3,color.red)
        art.pixel(18,eyeY-1,color.redGlint);art.pixel(27,eyeY-1,color.redGlint)
        art.offset()
    end,{foot_anchor=41})
end
art.animation("actors/yin_yang_orb",32,32,4,1,function(frame)
    art.layer("01 Orb silhouette and taiji halves");art.yinyang(16,16,11)
    art.layer("02 Jade rim and moving glint");art.ring(16,16,13,1,color.mist,1)
    local angle=frame*math.pi/2;art.star(16+math.cos(angle)*12,16+math.sin(angle)*12,2,color.white)
end,{foot_anchor=16})
end
