--Dependencies
local utils = require("Modules.Utilities")

local buzzerData = {
  enable = 0,
  frequency = 0,
  volumePercent = 100
}
local incrementAmount = 100

--boolean to turn off init screen
local initialized = false

--Create timer for ellipsis on initializing screen
local ellipsisTimer = gre.timer_set_interval(250,
  function() utils.AnimateEllipsis("Buzzer.InitializingGroup.InitializingText.initString") end)


---
--
local function PrintBuzzerData(data)
  gre.log(gre.LOG_INFO, "Buzzer Data")
  gre.log(gre.LOG_INFO, "\tenable: " .. tostring(data.enable))
  gre.log(gre.LOG_INFO, "\tfrequency: " .. tostring(data.frequency))
  gre.log(gre.LOG_INFO, "\tvolume: " .. tostring(data.volumePercent))
end

---
--
local function FEE_BuzzerRequest(enableRequest, frequencyRequest, volumePercentRequest)

  --Because the UI doesn't have volume control, we always want to set the buzzer to maximum volume
  volumePercentRequest = 100

  --Create buzzerRequest data
  local data = {
    enable = enableRequest,
    frequency = utils.RoundToNearest(frequencyRequest, incrementAmount),
    volumePercent = volumePercentRequest
  }
  
  --Send the data
  gre.send_event_data(
    "FEE_BuzzerRequest",
    "4u1 frequency 1u1 volumePercent 1u1 enable",
    data,
    BackendChannel
  )
  
end

---
--
function BEE_BuzzerStatus(mapargs)
  --Print status
  local message = "SB Buzzer Status"
  gre.log(gre.LOG_ALWAYS, message)
  PrintBuzzerData(mapargs.context_event_data)
  
  --Update local variables to match hardware's values
  buzzerData.enable = mapargs.context_event_data.enable
  buzzerData.frequency = mapargs.context_event_data.frequency
  buzzerData.volumePercent = mapargs.context_event_data.volumePercent
  
  --Display frequency on screen
  gre.set_value("Buzzer.Frequency.Frequency", mapargs.context_event_data.frequency)
  
  --turn off initializing screen
  if(not initialized) then gre.timer_clear_interval(ellipsisTimer)
    initialized = true
    utils.HideInitGroup("Buzzer.InitializingGroup")
  end
  

end

---
--
function BuzzerOn(mapargs)
  FEE_BuzzerRequest(1, buzzerData.frequency, buzzerData.volumePercent) 
  utils.ButtonHighlight("Buzzer.OnButton.Alpha")
  utils.ButtonDarken("Buzzer.OffButton.Alpha")
end

---
--
function BuzzerOff(mapargs)
  FEE_BuzzerRequest(0, buzzerData.frequency, buzzerData.volumePercent)
  utils.ButtonHighlight("Buzzer.OffButton.Alpha")
  utils.ButtonDarken("Buzzer.OnButton.Alpha")
end

---
--
function IncreaseFrequency(mapargs)
  FEE_BuzzerRequest(buzzerData.enable, buzzerData.frequency + incrementAmount, buzzerData.volumePercent)
  utils.ButtonHighlight("Buzzer.Plus.pressAlpha")
end

---
--
function DecreaseFrequency(mapargs)
  FEE_BuzzerRequest(buzzerData.enable, buzzerData.frequency - incrementAmount, buzzerData.volumePercent)
  utils.ButtonHighlight("Buzzer.Minus.pressAlpha")
end

---
--
function BuzzerPlusReleased(mapargs)
  utils.ButtonDarken("Buzzer.Plus.pressAlpha")
end

---
--
function BuzzerMinusReleased(mapargs)
  utils.ButtonDarken("Buzzer.Minus.pressAlpha")
end
