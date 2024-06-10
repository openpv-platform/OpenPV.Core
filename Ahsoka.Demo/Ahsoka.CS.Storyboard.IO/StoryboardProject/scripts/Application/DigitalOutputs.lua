--Dependencies
local utils = require("Modules.Utilities")

local digOut1State = -1
local digOut2State = -1

--booleans to turn off init screen
local initialized = {dig1 = false, dig2 = false}

--Create timer for ellipsis on init screen
local ellipsisTimer = gre.timer_set_interval(
  250,
  function() utils.AnimateEllipsis("DigitalOutput.InitializingGroup.InitializingText.initString") end
)


---
--
local function FEE_DigitalOutput1Request(request)
  --Get the desired output state
  local desiredState = request
  
  --Send the request
  gre.send_event_data(
    "FEE_DigitalOutput1Request",
    "4s1 state",
    {state = desiredState},
    BackendChannel
  )
  
  --Print request to console
  gre.log(gre.LOG_INFO, "SB DigOut1 Request: " .. tostring(desiredState))
end

---
--
local function FEE_DigitalOutput2Request(request)
  --Get the desired output state
  local desiredState = request
  
  --Send the request
  gre.send_event_data(
    "FEE_DigitalOutput2Request",
    "4s1 state",
    {state = desiredState},
    BackendChannel
  )
  
  --Print request to console
  gre.log(gre.LOG_INFO, "SB DigOut2 Request: " .. tostring(desiredState))
end

---
--
function BEE_DigitalOutput1Status(mapargs)
  --Get state from mapargs
  digOut1State = tostring(mapargs.context_event_data.state)
  
  --Print state to console
  local message = "SB DigOut1 Status: " .. digOut1State
  
  --Show state on screen
  gre.set_value("DigitalOutput.DigitalOutput1.DigitalOutput1Text", digOut1State)
  
  --set dig1 to true and potentially turn off initializing screen
  initialized.dig1 = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"DigitalOutput.InitializingGroup")
end

---
--
function BEE_DigitalOutput2Status(mapargs)
  --Get state from mapargs
  digOut2State = tostring(mapargs.context_event_data.state)
  
  --Print state to console
  local message = "SB DigOut2 Status: " .. digOut2State
  
  --Show state on screen
  gre.set_value("DigitalOutput.DigitalOutput2.DigitalOutput2Text", digOut2State)
  
  --set dig2 to true and potentially turn off initializing screen
  initialized.dig2 = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"DigitalOutput.InitializingGroup")
end

---
--
function DigitalOutput1On(mapargs)
 utils.ButtonHighlight("DigitalOutput.OnButton1.Alpha")
  utils.ButtonDarken("DigitalOutput.OffButton1.Alpha")
  FEE_DigitalOutput1Request(1)
end

---
--
function DigitalOutput1Off(mapargs)
 utils.ButtonHighlight("DigitalOutput.OffButton1.Alpha")
  utils.ButtonDarken("DigitalOutput.OnButton1.Alpha")
  FEE_DigitalOutput1Request(0)
end

---
--
function DigitalOutput2On(mapargs)
 utils.ButtonHighlight("DigitalOutput.OnButton2.Alpha")
  utils.ButtonDarken("DigitalOutput.OffButton2.Alpha")
  FEE_DigitalOutput2Request(1)
end

---
--
function DigitalOutput2Off(mapargs)
 utils.ButtonHighlight("DigitalOutput.OffButton2.Alpha")
  utils.ButtonDarken("DigitalOutput.OnButton2.Alpha")
  FEE_DigitalOutput2Request(0)
end
