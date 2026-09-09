return function(art)
local sheet=Image{fromFile=art.root.."/art/reference/shrine-props-generated-01.png"}
for _,entry in ipairs({
    {"shrine_main",{20,20,640,420},384,256,0.52},
    {"storehouse",{670,90,360,345},192,176,0.44},
    {"torii",{1045,100,480,320},224,160,0.30},
    {"tree_canopy_a",{20,455,550,545},176,184,0.68},
    {"tree_canopy_b",{576,453,520,548},176,184,0.68},
    {"lantern",{1190,556,193,359},64,112,0.45}
}) do
    local image=art.crop(sheet,entry[2],entry[3]-4,entry[4]-4,true)
    local canvas=Image(entry[3],entry[4],ColorMode.RGB)
    canvas:drawImage(image,Point(math.floor((canvas.width-image.width)/2),canvas.height-2-image.height))
    art.save("scenery/"..entry[1],canvas,{process="generated_reference_extraction_pixel_cleanup",reference="art/reference/shrine-props-generated-01.png",foot_anchor=canvas.height-2},{
        {name=entry[1]:find("tree") and "01 Canopy and branches" or "01 Roof and upper structure",accept=function(horizontal,vertical) return vertical<canvas.height*entry[5] end},
        {name=entry[1]:find("tree") and "02 Trunk and roots" or "02 Pillars facade and foundation",accept=function(horizontal,vertical) return vertical>=canvas.height*entry[5] end}
    })
end
local map=Image{fromFile=art.root.."/art/reference/shrine-courtyard-generated-02.png"}
local small=Image(map,Rectangle(1305,150,185,180))
local outline={{35,25},{86,6},{160,43},{162,64},{144,79},{143,119},{160,140},{105,170},{46,150},{12,122},{24,102},{27,78},{12,63},{24,51}}
local function inside(horizontal,vertical)
    local contained=false
    local previous=#outline
    for index=1,#outline do
        local point,prior=outline[index],outline[previous]
        if (point[2]>vertical)~=(prior[2]>vertical) and horizontal<(prior[1]-point[1])*(vertical-point[2])/(prior[2]-point[2])+point[1] then contained=not contained end
        previous=index
    end
    return contained
end
for iterator in small:pixels() do if not inside(iterator.x,iterator.y) then iterator(0) end end
small:resize(96,96)
art.save("scenery/subsidiary_shrine",small,{process="generated_map_silhouette_mask_pixel_cleanup",reference="art/reference/shrine-courtyard-generated-02.png",foot_anchor=91},{
    {name="01 Roof silhouette",accept=function(horizontal,vertical) return vertical<44 end},
    {name="02 Shrine and stone base",accept=function(horizontal,vertical) return vertical>=44 end}
})
end
