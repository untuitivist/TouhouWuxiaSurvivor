local root=app.params["root"] or app.fs.currentPath
local output=app.params["output"] or root.."/art/characters/reimu/touchup-01"
for _,name in ipairs({"touchup.aseprite","touchup.png","layers.json"}) do
    if app.fs.isFile(output.."/"..name) then error("Refusing to overwrite editable work: "..output.."/"..name) end
end
app.fs.makeAllDirectories(output)
local reference=app.open(root.."/art/reference/reimu-wuxia-approved.png")
local referenceImage=Image(reference.cels[1].image)
reference:close()
local sprite=Sprite(832,1216,ColorMode.RGB)
local art=dofile(root.."/tools/aseprite/reimu_anatomy_brush.lua")
local palette={ink="783444",skin="f4c6b4",skinLight="ffebd7",skinMid="e5a28f",skinShade="cd8079",skinDeep="a44d5b",cloth="f8eaeb",clothMid="e4cbd6",clothShade="c5a1b8",paper="fff4e0",paperMid="ebd8c7",paperShade="d1b4b1",red="cd2439",redLight="ed5960",redShade="983048",sole="d39161",soleLight="f2ba81",soleShade="9e5b54",shoe="452a46",shoeLight="8e597b"}
for name,hex in pairs(palette) do art.colors[name]=art.painter.color(hex) end
art.begin(sprite)
art.layer("01 Sleeve repair beneath forward hand - painted")
art.path({{577,674},{584,662,596,652,607,654},{620,652,631,654,642,658},{659,655,679,659,683,673},{688,686,678,701,665,710},{646,719,615,716,597,707},{585,706,577,694,577,674}},"clothMid")
art.path({{583,675},{599,658,621,657,642,664},{659,659,676,669,678,679},{680,693,665,704,649,707},{619,710,595,702,584,692}},"cloth")
art.path({{584,700},{607,710,626,713,646,712},{638,717,613,716,600,710}},"clothShade")
art.layer("02 Forward palm and curled fingers - painted")
art.path({{580,693},{583,682,591,674,602,673},{612,672,620,667,625,660},{631,653,640,650,643,654},{646,658,640,664,636,670},{645,666,654,668,661,673},{671,679,675,686,668,694},{660,703,646,707,634,708},{615,709,596,702,580,693}},"skin","ink",2)
art.path({{583,692},{599,700,620,705,634,704},{649,703,662,698,669,688},{669,695,658,703,647,706},{620,713,591,703,583,696}},"skinShade")
art.path({{587,688},{591,678,603,677,613,675},{621,674,629,668,634,661},{638,657,640,656,640,657},{639,663,629,673,624,678},{621,687,612,694,604,694},{597,694,591,692,587,688}},"skinLight")
art.path({{622,697},{634,700,647,698,655,691}},nil,"skinDeep",2)
art.path({{630,703},{640,704,648,701,653,698}},nil,"skinDeep")
art.path({{607,689},{615,690,621,693,626,694}},nil,"skinMid",2)
local function paper(name,corners)
    art.layer(name)
    art.path(corners,"paperMid","ink",2)
    local function point(horizontal,vertical)
        local upperX=corners[1][1]*(1-horizontal)+corners[2][1]*horizontal
        local upperY=corners[1][2]*(1-horizontal)+corners[2][2]*horizontal
        local lowerX=corners[4][1]*(1-horizontal)+corners[3][1]*horizontal
        local lowerY=corners[4][2]*(1-horizontal)+corners[3][2]*horizontal
        return {upperX*(1-vertical)+lowerX*vertical,upperY*(1-vertical)+lowerY*vertical}
    end
    local function path(points,color,fill,width)
        local transformed={}
        for _,coordinate in ipairs(points) do transformed[#transformed+1]=point(coordinate[1],coordinate[2]) end
        if fill then art.path(transformed,color) else art.path(transformed,nil,color,width or 2) end
    end
    path({{0.025,0.07},{0.97,0.045},{0.96,0.84},{0.04,0.92}},"paper",true)
    path({{0.02,0.91},{0.96,0.84},{0.99,0.94},{0.03,0.98}},"paperShade",true)
    path({{0.09,0.18},{0.89,0.15},{0.89,0.75},{0.12,0.82},{0.09,0.18}},"red",false,2)
    path({{0.13,0.22},{0.85,0.19}},"redLight",false)
    path({{0.17,0.28},{0.27,0.27},{0.29,0.64},{0.18,0.68},{0.17,0.28}},"red",false)
    path({{0.2,0.34},{0.26,0.36},{0.2,0.49},{0.26,0.56}},"red",false)
    path({{0.36,0.3},{0.4,0.69},{0.46,0.59},{0.43,0.47},{0.5,0.36}},"red",false)
    path({{0.33,0.4},{0.49,0.4}},"red",false)
    path({{0.34,0.59},{0.46,0.56}},"red",false)
    path({{0.56,0.31},{0.6,0.59},{0.56,0.7},{0.68,0.61},{0.65,0.43},{0.72,0.32}},"red",false)
    path({{0.53,0.43},{0.69,0.39}},"red",false)
    path({{0.77,0.28},{0.81,0.34},{0.79,0.65}},"red",false)
    path({{0.73,0.47},{0.85,0.46}},"red",false)
end
paper("03 Forward ofuda - clean paper border and abstract seal",{{622,663},{701,571},{749,608},{640,683}})
art.layer("04 Pinch thumb over ofuda - painted")
art.path({{638,695},{632,693,627,688,626,683},{625,678,629,673,633,672},{638,671,641,676,644,678},{649,680,655,676,659,677},{663,678,663,682,659,685},{650,687,646,694,638,695}},"skin","ink",2)
art.path({{629,683},{629,677,633,674,636,676},{639,680,643,682,647,682},{647,685,642,686,637,687},{633,688,631,685,629,683}},"skinLight")
art.path({{635,691},{643,690,648,684,654,683},{652,689,644,694,638,694}},"skinMid")
art.path({{630,678},{632,676,634,676,636,678}},nil,"skinMid")
paper("05 Rear ofuda - clean paper border and abstract seal",{{66,501},{161,510},{175,536},{88,555}})
art.layer("06 Rear grip separation - painted")
art.path({{158,516},{169,514,181,514,191,515},{203,516,216,518,225,517},{224,521,214,522,205,521},{184,520,170,521,159,521}},"skinLight","skinDeep")
art.path({{162,521},{177,522,190,520,201,522}},nil,"skinMid")
art.path({{172,518},{184,518,197,519,208,519}},nil,"skin")
art.layer("07 Forward shoe sole and contact edge - painted")
art.path({{618,1158},{643,1159,674,1159,699,1157},{702,1160,701,1166,696,1168},{674,1172,642,1171,620,1167}},"soleShade","shoe",2)
art.path({{621,1160},{646,1162,674,1162,697,1159},{698,1163,695,1165,689,1165},{666,1168,642,1167,623,1165}},"sole")
art.path({{625,1161},{645,1163,673,1163,694,1161}},nil,"soleLight",2)
art.path({{701,1158},{710,1148,726,1135,738,1123}},nil,"soleShade",2)
art.path({{705,1157},{715,1147,727,1137,737,1129}},nil,"soleLight")
art.path({{619,1149},{643,1151,672,1150,692,1148}},nil,"shoe",2)
art.path({{623,1146},{644,1148,661,1147,672,1146}},nil,"shoeLight")
art.finish()
local base=sprite:newLayer()
base.name="00 Approved AI base - NOT a full redraw"
base.stackIndex=1
sprite:newCel(base,1,referenceImage,Point(0,0))
base.isEditable=false
local original=sprite:newLayer()
original.name="Reference - approved original - hidden"
original.stackIndex=1
sprite:newCel(original,1,referenceImage,Point(0,0))
original.isVisible=false
original.isEditable=false
sprite:saveAs(output.."/touchup.aseprite")
sprite:saveCopyAs(output.."/touchup.png")
local manifest=io.open(output.."/layers.json","wb")
manifest:write('{"schema":1,"status":"partial_aseprite_touchup_not_full_redraw","width":832,"height":1216,"layers":[')
for index,layer in ipairs(sprite.layers) do
    if index>1 then manifest:write(",") end
    manifest:write(string.format('{"name":"%s","visible":%s,"editable":%s}',layer.name,tostring(layer.isVisible),tostring(layer.isEditable)))
end
manifest:write(']}\n')
manifest:close()
sprite:close()
print("REIMU_APPROVED_TOUCHUP_PASS "..output)
