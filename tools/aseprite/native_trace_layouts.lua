return function(hero,engine)
local pixel=app.pixelColor
local layout={}
local reimuBody={{242,177},{268,188},{297,208},{323,239},{344,217},{423,214},{421,246},{402,253},{399,289},{378,309},{399,308},{427,316},{456,312},{457,333},{432,357},{418,370},{443,376},{473,397},{508,422},{515,450},{500,465},{483,456},{487,483},{465,514},{439,526},{464,542},{490,572},{506,600},{504,616},{487,627},{463,621},{449,625},{455,644},{476,675},{477,691},{452,684},{438,665},{425,665},{418,639},{405,639},{394,663},{386,690},{349,691},{328,679},{313,711},{302,732},{283,762},{279,777},{270,779},{276,792},{296,803},{296,816},{286,831},{297,850},{313,889},{302,908},{282,918},{256,914},{247,902},{251,881},{239,859},{220,870},{206,859},{209,838},{222,803},{231,786},{216,780},{199,764},{183,749},{180,733},{170,760},{157,773},{150,752},{138,733},{122,710},{119,686},{138,648},{130,644},{111,650},{92,675},{91,644},{109,618},{101,611},{84,625},{62,643},{69,609},{93,578},{86,561},{77,553},{48,550},{29,542},{23,538},{29,515},{60,475},{93,440},{101,421},{79,416},{83,400},{108,379},{120,370},{116,358},{121,344},{137,329},{155,327},{171,337},{159,350},{151,364},{153,382},{167,390},{173,380},{191,383},{214,380},{206,371},{191,376},{192,356},{201,337},{214,317},{220,295},{210,285},{209,244},{221,229},{229,224},{232,208},{220,206},{222,190},{236,190},{234,178}}
local marisaBody={{278,146},{297,151},{315,168},{331,180},{354,211},{352,240},{373,228},{403,214},{418,215},{417,251},{424,267},{426,289},{451,302},{464,325},{466,351},{482,354},{505,372},{500,387},{478,377},{468,374},{477,398},{483,421},{505,434},{511,456},{504,478},{515,488},{510,511},{493,514},{478,510},{469,511},{460,498},{454,508},{465,531},{476,544},{485,569},{499,595},{505,613},{503,633},{491,646},{474,631},{459,624},{448,612},{427,608},{437,635},{452,659},{463,684},{461,706},{442,719},{432,690},{418,680},{412,675},{407,699},{416,717},{410,742},{389,746},{374,729},{360,726},{342,747},{322,760},{295,772},{282,778},{283,792},{297,808},{300,829},{291,842},{300,864},{309,888},{308,906},{283,917},{254,916},{244,900},{243,881},{234,866},{218,873},{199,864},{202,847},{212,811},{216,797},{208,785},{207,766},{192,755},{172,722},{164,700},{151,717},{135,735},{123,749},{112,780},{98,802},{87,803},{85,779},{96,751},{107,728},{109,699},{111,680},{94,677},{77,693},{56,712},{40,724},{24,716},{32,693},{47,674},{27,681},{19,674},{33,651},{50,631},{53,608},{49,583},{57,558},{48,560},{50,542},{77,505},{91,483},{104,463},{107,447},{117,434},{111,421},{101,405},{98,384},{104,362},{116,346},{132,339},{143,340},{139,328},{125,319},{109,296},{111,281},{134,263},{158,254},{169,240},{160,232},{173,219},{182,199},{208,193},{223,179},{244,166},{262,150}}
local body=hero=="reimu" and reimuBody or marisaBody
local heldOfuda={{144,242},{184,247},{164,333},{126,320}}
function layout.figure(horizontal,vertical,value)
    return engine.inside(horizontal,vertical,body) or hero=="reimu" and engine.inside(horizontal,vertical,heldOfuda)
end
function layout.portraitPart(horizontal,vertical,value)
    if not layout.figure(horizontal,vertical,value) or engine.isBackdrop(value) then return "02 Portrait environment" end
    if hero=="reimu" then
        if vertical<330 and horizontal<189 then return "15 Held ofuda" end
        if vertical<313 then return "04 Hair ribbon and headwear" end
        if horizontal>313 and vertical<532 then return "03 Rear hair and ribbons" end
        if vertical<397 and horizontal>180 then return "05 Face and front hair" end
        if horizontal<199 and vertical<586 then return "08 Raised arm and detached sleeve" end
        if horizontal>334 and vertical<670 then return "09 Open hand and detached sleeve" end
        if vertical>=769 then return "14 Boots straps and sole" end
        if vertical>598 and horizontal>178 and horizontal<310 then return "13 Trousers and lower legs" end
        if vertical>=482 and vertical<590 and horizontal>231 and horizontal<356 then return "11 Belt seal and sash" end
        return "10 Collar tunic and split tails"
    end
    if vertical<322 then return "04 Witch hat and white bow" end
    if horizontal>=318 and vertical<537 then return "03 Rear hair braid and ribbons" end
    if vertical<408 and horizontal>=184 then return "05 Face and front hair" end
    if horizontal<181 and vertical<451 then return "15 Mini hakkero and holding hand" end
    if horizontal<224 and vertical<552 then return "08 Raised arm and white sleeve" end
    if horizontal>=364 and vertical<646 then return "09 Hand gesture and white sleeve" end
    if horizontal<156 and vertical>=545 then return "16 Broom and herb accessories" end
    if vertical>=770 then return "14 Boots straps and sole" end
    if vertical>618 and horizontal>=171 and horizontal<=325 then return "13 Trousers and lower legs" end
    if vertical>=477 and vertical<616 and horizontal>=153 and horizontal<=344 then return "11 Belt furnace straps and satchel" end
    return "10 Collar robe and split tails"
end
function layout.boardLayer(horizontal,vertical,value)
    if vertical<100 then return "00 Header lettering and scene motifs" end
    if horizontal<538 then
        if vertical>936 then return "19 Palette and caption" end
        if horizontal>350 and vertical>=727 and vertical<900 then return "18 Large gameplay sprite reference" end
        if vertical<147 then return "01 Portrait section heading" end
        return layout.portraitPart(horizontal,vertical,value)
    end
    if vertical<472 then
        return horizontal<1014 and "20 Casting scene - left" or "21 Casting scene - right"
    end
    if vertical<690 then
        if vertical<503 or horizontal<704 then return "30 Movement table and headings" end
        local row=vertical<563 and "front" or vertical<623 and "side" or "back"
        local column=math.min(4,math.floor((horizontal-704)/211)+1)
        return "31 Movement "..row.." "..column
    end
    if horizontal<690 or vertical<721 or vertical>981 then return "40 Effects table and headings" end
    local row=vertical<796 and 1 or vertical<884 and 2 or 3
    local column=math.min(4,math.floor((horizontal-690)/211)+1)
    return "4"..row.." Effect "..column
end
layout.portrait=Rectangle(18,147,506,779)
layout.spriteColumns={704,916,1127,1338}
layout.spriteRows={503,563,623}
return layout
end
