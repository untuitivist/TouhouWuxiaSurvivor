local sprite = Sprite(24, 24, ColorMode.RGB)
sprite.layers[1].name = "guide"
sprite.layers[1].isVisible = false
local palette = {
  ["o"] = Color{r=45,g=39,b=61,a=255},
  ["c"] = Color{r=127,g=99,b=169,a=255},
  ["h"] = Color{r=202,g=179,b=222,a=255},
  ["s"] = Color{r=242,g=224,b=184,a=255},
  ["t"] = Color{r=181,g=148,b=117,a=255},
  ["l"] = Color{r=104,g=165,b=130,a=255},
  ["v"] = Color{r=171,g=211,b=151,a=255}
}
local pattern = {
  "........................",
  "........................",
  "..........oooo..........",
  "........oohhhhoo........",
  ".......ohhcccccho.......",
  "......ohcccssccccho.....",
  ".....ohcccssscccccho....",
  "....ohhccccscccshccho...",
  "....ohcccccccccsccho....",
  "...ohccccccccccccccco...",
  "...occcchhccccchhccco...",
  "....oosssssssssssoo.....",
  "......oottttttoo........",
  "........otstto..........",
  "........otstto....ov....",
  ".....ol..otsto..olvo....",
  ".....ovlootsstoovlvo....",
  "......ovlotstolvlo......",
  ".......ovottovlvo.......",
  "........oooooo..........",
  "........................",
  "........................",
  "........................",
  "........................"
}
local groups = { {name="outline", codes="o"}, {name="stem", codes="st"}, {name="cap", codes="ch"}, {name="herbs", codes="lv"} }
for _, group in ipairs(groups) do
  local layer = sprite:newLayer()
  layer.name = group.name
  local image = Image(24, 24, ColorMode.RGB)
  for row, pixels in ipairs(pattern) do
    for column = 1, math.min(#pixels, 24) do
      local code = pixels:sub(column, column)
      if group.codes:find(code, 1, true) then image:drawPixel(column - 1, row - 1, palette[code]) end
    end
  end
  sprite:newCel(layer, 1, image, Point(0, 0))
end
sprite:saveAs(app.params["source"])
app.command.SaveFileCopyAs{ui=false,filename=app.params["output"]}
print("MARISA_REMEDY_ASEPRITE_PASS")
