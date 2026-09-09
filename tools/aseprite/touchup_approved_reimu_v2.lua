local root=app.params["root"] or app.fs.currentPath
local output=app.params["output"] or root.."/art/characters/reimu/touchup-02"
for _,name in ipairs({"touchup.aseprite","touchup.png","layers.json"}) do
    if app.fs.isFile(output.."/"..name) then error("Refusing to overwrite editable work: "..output.."/"..name) end
end
app.fs.makeAllDirectories(output)
local reference=app.open(root.."/art/reference/reimu-wuxia-approved.png")
local referenceImage=Image(reference.cels[1].image)
reference:close()
local sprite=Sprite(832,1216,ColorMode.RGB)
local art=dofile(root.."/tools/aseprite/reimu_anatomy_brush.lua")
for name,hex in pairs({paper="fff9f1",paperShade="f2dcde",red="d82a3d",redLight="ef6770",redShade="a82b48",sole="d78e5d",soleLight="f2b37b",soleShade="ab6653"}) do art.colors[name]=art.painter.color(hex) end
art.begin(sprite)
art.layer("01 Forward ofuda face - painted cleanup")
art.path({{639,653},{654,642,671,626,685,610},{697,598,701,586,706,580},{718,583,733,594,741,606},{730,616,706,626,689,635},{671,644,653,651,639,653}},"paper")
art.path({{644,651},{667,642,686,631,704,624},{721,617,733,610,740,606},{735,612,718,624,694,634},{670,645,653,650,644,651}},"paperShade")
art.path({{708,584},{718,587,729,596,735,603}},nil,"red",2)
art.path({{649,647},{672,633,691,616,704,590}},nil,"red",2)
art.path({{650,649},{677,641,704,626,734,608}},nil,"red",2)
art.layer("02 Forward ofuda seal marks - painted")
art.path({{708,592},{718,595,724,600,727,604},{722,609,716,612,711,615},{704,611,700,607,698,602},{702,599,705,596,708,592}},nil,"red",2)
art.path({{708,598},{713,601,717,603,721,605}},nil,"red",2)
art.path({{708,600},{707,607,711,610,714,610}},nil,"red",2)
art.path({{705,605},{714,601}},nil,"red",2)
art.path({{690,611},{697,614,700,618,704,620}},nil,"red",2)
art.path({{698,608},{694,617,690,622,685,626}},nil,"red",3)
art.path({{687,618},{698,615}},nil,"red",2)
art.path({{685,627},{690,626,695,623,698,622}},nil,"red",2)
art.path({{678,625},{683,628,685,631,687,633}},nil,"red",2)
art.path({{685,623},{680,632,675,636,671,639}},nil,"red",2)
art.path({{675,632},{684,629}},nil,"red",2)
art.path({{659,640},{667,635,670,638,667,641},{665,642}},nil,"red",2)
art.line(656,643,659,642,"red",2)
art.layer("03 Rear ofuda face and seal - painted cleanup")
art.path({{75,510},{94,515,116,516,143,516},{146,521,150,526,154,531},{131,537,109,544,91,547},{83,535,79,522,75,510}},"paper")
art.path({{91,544},{111,540,133,534,151,529},{153,531},{131,538,109,544,92,548}},"paperShade")
art.path({{80,513},{97,518,116,519,139,519}},nil,"red",2)
art.path({{82,517},{89,533},{95,542}},nil,"red",2)
art.path({{95,542},{112,539,134,533,147,530}},nil,"red",2)
art.path({{90,521},{100,521},{104,535},{95,537},{90,521}},nil,"red",2)
art.path({{94,525},{101,527},{96,530},{102,533}},nil,"red",2)
art.path({{111,521},{116,535}},nil,"red",2)
art.path({{106,527},{120,525}},nil,"red",2)
art.path({{110,532},{120,528},{123,532}},nil,"red",2)
art.path({{126,521},{130,531},{137,525}},nil,"red",2)
art.path({{121,526},{136,522}},nil,"red",2)
art.line(139,525,142,529,"red",2)
art.layer("04 Forward shoe sole highlights - painted cleanup")
art.path({{623,1160},{645,1162,674,1162,696,1159},{697,1162,693,1165,688,1165},{665,1168,644,1167,625,1165}},"sole")
art.path({{627,1161},{647,1163,673,1163,691,1161}},nil,"soleLight",2)
art.path({{627,1166},{647,1168,671,1168,687,1166}},nil,"soleShade")
art.path({{705,1156},{717,1143,727,1137,736,1129}},nil,"sole",2)
art.path({{710,1150},{720,1142,727,1136,731,1133}},nil,"soleLight")
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
