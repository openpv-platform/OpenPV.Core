--Dependencies
local utils = require("Modules.Utilities")

--booleans to turn off system init screen
local initialized = {in1 = false, in2 = false, in3 = false, in4 = false}

--Create timer for ellipsis on init screen
local ellipsisTimer = gre.timer_set_interval(
  250,
  function() utils.AnimateEllipsis("AnalogIO.InitializingGroup.InitializingText.initString") end
)

---
--
local function ToVolts(input) 
  --Function to translate analog readings from millivolts into volts and then into a string
  --Divides raw input by 1000 to convert to volts, then rounds to nearest 0.1 to shorten the number of digits after the decimal
  return tostring(utils.RoundToNearest((input/1000), 0.1)) .. " Volts"
end

---
--
function BEE_AnalogInput1Status(mapargs)
  --Get input status from mapargs
  local inputStatus = ToVolts(mapargs.context_event_data.value)
  
  --Send input status to screen
  gre.set_value("AnalogIO.AnalogInput1.AnalogInput1Text", inputStatus)
  
  --set in1 to true and potentially turn off initializing screen
  initialized.in1 = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"AnalogIO.InitializingGroup")
end

---
--
function BEE_AnalogInput2Status(mapargs)
  --Get input status from mapargs
  local inputStatus = ToVolts(mapargs.context_event_data.value)

  --Send input status to screen
  gre.set_value("AnalogIO.AnalogInput2.AnalogInput2Text", inputStatus)
  
  --set in2 to true and potentially turn off initializing screen
  initialized.in2 = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"AnalogIO.InitializingGroup")
end

---
--
function BEE_DigitalInput1Status(mapargs)
  --While this function is labeled as Digital Input, the value is between 0-5 volts, so we're treating it as Analog Input #3
  local inputStatus = ToVolts(mapargs.context_event_data.value)
  
  --Send input status to screen
  gre.set_value("AnalogIO.AnalogInput3.AnalogInput3Text", inputStatus)
  
  --set in3 to true and potentially turn off initializing screen
  initialized.in3 = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"AnalogIO.InitializingGroup")
end

---
--
function BEE_DigitalInput2Status(mapargs)
  --While this function is labeled as Digital Input, the value is between 0-5 volts, so we're treating it as Analog Input #4
  local inputStatus = ToVolts(mapargs.context_event_data.value)

  --Send input status to screen
  gre.set_value("AnalogIO.AnalogInput4.AnalogInput4Text", inputStatus)
  
  --set in4 to true and potentially turn off initializing screen
  initialized.in4 = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"AnalogIO.InitializingGroup")
end
