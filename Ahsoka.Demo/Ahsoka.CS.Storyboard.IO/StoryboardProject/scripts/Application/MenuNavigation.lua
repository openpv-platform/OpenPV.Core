--Define the available layers
local layers = {
  "DigitalOutput",
  "Buzzer",
  "Brightness",
  "SystemInfo",
  "VersionInfo",
  "AnalogIO",
  "ShutDown"
}


---
--
local function SetActiveLayer(activeLayer)

  --Initialize screen data
  local data = {}

  --Loop through the layers
  for index, layer in pairs(layers) do
  
    --Actvie
    if layer == activeLayer then
      --Highlight the menu item
      data["NavMenu.NavMenu.HighlightedAlpha." .. index .. ".1"] = 255
      
      --Show the layer
      data["Screen." .. layer .. ".grd_hidden"] = 0 
    
    --Inactive
    else
      --Unhighlight the menu item
      data["NavMenu.NavMenu.HighlightedAlpha." .. index .. ".1"] = 0
      
      --Hide the layer
      data["Screen." .. layer .. ".grd_hidden"] = 1 
    
    end
    
  end
  
  --Set the screen data
  gre.set_data(data)

end


--- @param gre#context mapargs
function LayerTransition(mapargs)

  SetActiveLayer(layers[mapargs.context_row])

end