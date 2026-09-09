local root=app.params["root"] or app.fs.currentPath
local edition=app.params["edition"] or "shrine-v04"
assert(edition:match("^[a-z0-9-]+$"),"ASCII edition required")
local painter=dofile(root.."/tools/aseprite/pixel_tools.lua")
local art={root=root,edition=edition,entries={},images={},offsetX=0,offsetY=0}
art.sourceRoot=root.."/art/"..edition
art.textureRoot=root.."/assets/aseprite/"..edition
art.previewRoot=root.."/artifacts/"..edition
art.palette={}
art.hex={ink="170d12",inkSoft="29171d",hairDark="48252b",hairShade="663536",hair="773f40",hairMid="884b49",hairLight="9b6966",hairGlint="b58073",redDark="660e20",redShade="b71429",red="e72737",redLight="ee394d",redGlint="ff7979",redEdge="ba5e65",white="fcf6f5",paper="f3e4e3",clothMid="d4c3ce",clothShade="ad969e",clothDark="776577",skinLight="f9e6d0",skin="fbdec9",skinMid="efb894",skinShade="d98967",goldLight="ffe074",gold="f9982b",goldShade="c65e1e",trousers="26191b",trousersLight="482c2d",boot="350711",sole="9c302e",night="142638",deep="1c3042",blue="2b4559",slate="476275",mist="7b94a2",stoneDark="474c50",stone="646b6b",stoneLight="92958b",moss="5b6954",jade="7d9670",mint="c2cf9c",bark="553f3b",wood="926448",woodLight="c79868",cream="fff2d4",blondDark="94602f",blond="dba154",blondLight="f7cf79",hat="292532",hatLight="494050",hatMid="36303f",cyan="73e6fc",cyanLight="d8fdff",violet="7252b5",lavender="b9a6ed"}
for name,hex in pairs(art.hex) do art.palette[name]=painter.color(hex) end
function art.color(hex,alpha) return painter.color(hex,alpha) end
function art.offset(horizontal,vertical) art.offsetX=horizontal or 0;art.offsetY=vertical or 0 end
function art.layer(name) painter.layer(name) end
function art.pixel(horizontal,vertical,color) painter.pixel(horizontal+art.offsetX,vertical+art.offsetY,color) end
function art.rect(left,top,width,height,color) painter.rect(left+art.offsetX,top+art.offsetY,width,height,color) end
function art.line(left,top,right,bottom,color,thickness) painter.line(left+art.offsetX,top+art.offsetY,right+art.offsetX,bottom+art.offsetY,color,thickness) end
function art.ellipse(horizontal,vertical,width,height,color) painter.ellipse(horizontal+art.offsetX,vertical+art.offsetY,width,height,color) end
function art.poly(points,color)
    local translated={}
    for _,point in ipairs(points) do translated[#translated+1]={point[1]+art.offsetX,point[2]+art.offsetY} end
    painter.poly(translated,color)
end
function art.cluster(horizontal,vertical,width,height,color) painter.cluster(horizontal+art.offsetX,vertical+art.offsetY,width,height,color) end
function art.ring(horizontal,vertical,radius,aspect,color,thickness)
    local previousX,previousY=horizontal+radius,vertical
    for step=1,80 do
        local angle=step*math.pi*2/80
        local nextX,nextY=horizontal+math.cos(angle)*radius,vertical+math.sin(angle)*radius*aspect
        art.line(previousX,previousY,nextX,nextY,color,thickness)
        previousX,previousY=nextX,nextY
    end
end
function art.star(horizontal,vertical,radius,color,angle)
    local points={}
    for index=0,9 do
        local phase=-math.pi/2+index*math.pi/5+(angle or 0)
        local length=index%2==0 and radius or radius*0.43
        points[#points+1]={horizontal+math.cos(phase)*length,vertical+math.sin(phase)*length}
    end
    art.poly(points,color)
end
function art.yinyang(horizontal,vertical,radius)
    local color=art.palette
    art.ellipse(horizontal,vertical,radius+2,radius+2,color.ink)
    art.ellipse(horizontal,vertical,radius,radius,color.paper)
    art.poly({{horizontal,vertical-radius},{horizontal+radius-1,vertical-radius/2},{horizontal+radius,vertical+radius/2},{horizontal,vertical+radius},{horizontal,vertical}},color.hat)
    art.ellipse(horizontal,vertical-radius/2,radius/2,radius/2,color.paper)
    art.ellipse(horizontal,vertical+radius/2,radius/2,radius/2,color.hat)
    art.ellipse(horizontal,vertical-radius/2,math.max(1,radius/6),math.max(1,radius/6),color.hat)
    art.ellipse(horizontal,vertical+radius/2,math.max(1,radius/6),math.max(1,radius/6),color.paper)
end
function art.flatten(sprite,frame)
    local image=Image(sprite.width,sprite.height,ColorMode.RGB)
    image:drawSprite(sprite,frame or 1)
    return image
end
local function record(name,sprite,image,metadata)
    metadata=metadata or {}
    metadata.name=name;metadata.width=image.width;metadata.height=image.height
    metadata.source_width=sprite.width;metadata.source_height=sprite.height;metadata.frames=#sprite.frames
    metadata.source="art/"..edition.."/"..name..".aseprite"
    metadata.texture="assets/aseprite/"..edition.."/"..name..".png"
    metadata.method=metadata.method or "authored_clusters_in_aseprite"
    metadata.manual_mouse_painting=false;metadata.reference_pixels_in_export=false
    metadata.layers={}
    for _,layer in ipairs(sprite.layers) do metadata.layers[#metadata.layers+1]={name=layer.name,visible=layer.isVisible,editable=layer.isEditable} end
    art.entries[#art.entries+1]=metadata;art.images[name]=image
end
function art.finish(name,sprite,image,metadata)
    local source=art.sourceRoot.."/"..name..".aseprite"
    local texture=art.textureRoot.."/"..name..".png"
    assert(not app.fs.isFile(source),"Refusing to overwrite source: "..source)
    app.fs.makeAllDirectories(app.fs.filePath(source))
    app.fs.makeAllDirectories(app.fs.filePath(texture))
    sprite.data=json.encode({method="authored_aseprite_drawings",manual_mouse_painting=false,reference_pixels_in_export=false,edition=edition})
    sprite:saveAs(source);image:saveAs(texture)
    record(name,sprite,image,metadata);sprite:close()
    print("ASEPRITE_COMPLETE_ASSET "..name.." "..image.width.."x"..image.height)
end
function art.save(name,width,height,draw,metadata)
    local sprite=Sprite(width,height,ColorMode.RGB)
    art.offset();painter.begin(sprite);draw();painter.finish()
    art.finish(name,sprite,art.flatten(sprite),metadata)
end
function art.animation(name,width,height,columns,rows,draw,metadata)
    metadata=metadata or {}
    metadata.columns=columns;metadata.rows=rows;metadata.frame_width=width;metadata.frame_height=height
    local sprite=Sprite(width,height,ColorMode.RGB)
    local sheet=Image(width*columns,height*rows,ColorMode.RGB)
    local layers={}
    local initialized=false
    for row=0,rows-1 do
        for column=0,columns-1 do
            local frameIndex=row*columns+column+1
            if frameIndex>1 then sprite:newEmptyFrame(frameIndex) end
            sprite.frames[frameIndex].duration=columns==8 and (column==0 and 0.25 or column==6 and 0.07 or 0.11) or 0.12
            local cell=Sprite(width,height,ColorMode.RGB)
            art.offset();painter.begin(cell);draw(column,row);painter.finish()
            sheet:drawImage(art.flatten(cell),Point(column*width,row*height))
            for _,cel in ipairs(cell.cels) do
                local target=layers[cel.layer.name]
                if not target then
                    target=initialized and sprite:newLayer() or sprite.layers[1]
                    initialized=true;target.name=cel.layer.name;layers[target.name]=target
                end
                sprite:newCel(target,frameIndex,Image(cel.image),cel.position)
            end
            cell:close()
        end
    end
    for row=0,rows-1 do
        if columns==8 then
            local prefix=({"front","right","back","left"})[row+1]
            sprite:newTag(row*columns+1,row*columns+1).name=prefix.."_idle"
            sprite:newTag(row*columns+2,row*columns+5).name=prefix.."_walk"
            sprite:newTag(row*columns+6,row*columns+8).name=prefix.."_cast"
        else sprite:newTag(row*columns+1,(row+1)*columns).name="loop_"..row end
    end
    if metadata.reference then
        local board=Image{fromFile=root.."/"..metadata.reference}
        local guide=sprite:newLayer();guide.name="REFERENCE ONLY - hidden locked"
        sprite:newCel(guide,1,Image(board,Rectangle(704,503,112,60)),Point(-20,4))
        guide.isVisible=false;guide.isEditable=false
    end
    art.finish(name,sprite,sheet,metadata)
end
return art
