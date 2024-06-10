--Dependencies
local utils = require("Modules.Utilities")

local partNumber
local serialNumber
local applicationName
local deviceId


--booleans to turn off system init screen
local initialized = {part = false, serial = false, name = false, id = false}

--Create timer for ellipsis on init screen
local ellipsisTimer = gre.timer_set_interval(
  250,
  function() utils.AnimateEllipsis("SystemInfo.InitializingGroup.InitializingText.initString") end
)


---
--
function GetSystemInfo(mapargs)
  utils.ButtonHighlight("SystemInfo.GetStringsButton.pressAlpha")
  
  --Populate local variables
  FEE_SystemInfoRequest()
end

---
--
function FEE_SystemInfoRequest()
  --Log request to console
  gre.log(gre.LOG_INFO, "SB System Info Request")
  
  --Request system info
  gre.send_event(
    "FEE_SystemInfoRequest",
    BackendChannel
  )
end

---
--
function SystemButtonReleased(mapargs)
  utils.ButtonDarken("SystemInfo.GetStringsButton.pressAlpha")
end


--System Info Requests
--These functions update text on screen with the system info
---
--
function BEE_PartNumber(mapargs)
  partNumber = tostring(mapargs.context_event_data.partNumber)
  gre.set_value("SystemInfo.SystemInfoTable.InfoText.1.1", partNumber)
  
  --set part to true and potentially turn off initializing screen
  initialized.part = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"SystemInfo.InitializingGroup")
end

---
--
function BEE_SerialNumber(mapargs)
  serialNumber = tostring(mapargs.context_event_data.serialNumber)
  gre.set_value("SystemInfo.SystemInfoTable.InfoText.2.1", serialNumber)
  
  --set serial to true and potentially turn off initializing screen
  initialized.serial = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"SystemInfo.InitializingGroup")
end

---
--
function BEE_ApplicationName(mapargs)
  applicationName = tostring(mapargs.context_event_data.applicationName)
  gre.set_value("SystemInfo.SystemInfoTable.InfoText.3.1", applicationName)
  
  --set name to true and potentially turn off initializing screen
  initialized.name = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"SystemInfo.InitializingGroup")
end

---
--
function BEE_DeviceId(mapargs)
  deviceId = tostring(mapargs.context_event_data.deviceId)
  gre.set_value("SystemInfo.SystemInfoTable.InfoText.4.1", deviceId)
  
  --set id to true and potentially turn off initializing screen
  initialized.id = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"SystemInfo.InitializingGroup")
end
