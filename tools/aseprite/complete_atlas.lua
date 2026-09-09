return function(art)
local entries={}
for _,entry in ipairs(art.entries) do if entry.foot_anchor then entries[#entries+1]=entry end end
table.sort(entries,function(left,right)if left.height==right.height then return left.name<right.name end return left.height>right.height end)
local horizontal,vertical,rowHeight=2,2,0
local sprites={}
for _,entry in ipairs(entries) do
    if horizontal+entry.width+2>1024 then horizontal=2;vertical=vertical+rowHeight+4;rowHeight=0 end
    sprites[#sprites+1]={name=entry.name,x=horizontal,y=vertical,columns=entry.columns or 1,rows=entry.rows or 1,frame_width=entry.frame_width or entry.width,frame_height=entry.frame_height or entry.height,foot_anchor=entry.foot_anchor}
    horizontal=horizontal+entry.width+4;rowHeight=math.max(rowHeight,entry.height)
end
local height=64;while height<vertical+rowHeight+2 do height=height*2 end
local sprite=Sprite(1024,height,ColorMode.RGB)
for index,entry in ipairs(sprites) do
    local layer=index==1 and sprite.layers[1] or sprite:newLayer();layer.name=entry.name
    sprite:newCel(layer,1,art.images[entry.name],Point(entry.x,entry.y))
end
art.finish("atlas/actors",sprite,art.flatten(sprite),{method="atlas_from_new_editable_sources"})
local file=assert(io.open(art.textureRoot.."/atlas/actors.json","wb"));file:write(json.encode({schema=1,width=1024,height=height,sprites=sprites}).."\n");file:close()
end
