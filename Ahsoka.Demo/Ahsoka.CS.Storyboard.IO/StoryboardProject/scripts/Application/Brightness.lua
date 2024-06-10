--Dependencies
local utils = require("Modules.Utilities")

local incrementAmount = 5
local currentBrightness = nil


--boolean to turn off init screen
local initialized = false

--Create timer for ellipsis on initializing screen
local ellipsisTimer = gre.timer_set_interval(250,
  function() utils.AnimateEllipsis("Brightness.InitializingGroup.InitializingText.initString") end)


---
--
local function FEE_BrightnessRequest(requestedBrightness)

  --Create brightnessRequest data
  local data = {
    percent = utils.RoundToNearest(requestedBrightness, incrementAmount)
  }
  
  --Send the data
  gre.send_event_data(
    "FEE_BrightnessRequest",
    "1u1 percent",
    data,
    BackendChannel
  )
  
  --Print request
  local message = "SB Brightness Request: " .. tostring(data.percent)
  gre.log(gre.LOG_ALWAYS, message)
  
end

---
--
function BEE_BrightnessStatus(mapargs)
  --Save brightness to persistent variable
  currentBrightness = mapargs.context_event_data.percent
  
  --Print status
  local message = "SB Brightness Status: " .. tostring(currentBrightness)
  gre.log(gre.LOG_ALWAYS, message)

  --Update Screen
  gre.set_value("Brightness.BrightnessText.BrightnessText", tostring(currentBrightness))
  
  
  --turn off initializing screen
  if(not initialized) then gre.timer_clear_interval(ellipsisTimer)
    initialized = true
    utils.HideInitGroup("Brightness.InitializingGroup")
  end
  
end

---
--
function IncreaseBrightness(mapargs)
  utils.ButtonHighlight("Brightness.Plus.pressAlpha")

  --Use local brightness variable to calculate a requested brightness
  if currentBrightness then
    local requestedBrightness = currentBrightness + incrementAmount
    FEE_BrightnessRequest(requestedBrightness)
  end
  
end

---
--
function DecreaseBrightness(mapargs)
  utils.ButtonHighlight("Brightness.Minus.pressAlpha")

  --Use local brightness variable to calculate a requested brightness
  if currentBrightness then
    local requestedBrightness = currentBrightness - incrementAmount
    FEE_BrightnessRequest(requestedBrightness)
  end
  
end

---
--
function BrightnessPlusReleased(mapargs)
  utils.ButtonDarken("Brightness.Plus.pressAlpha")
end

---
--
function BrightnessMinusReleased(mapargs)
  utils.ButtonDarken("Brightness.Minus.pressAlpha")
end
