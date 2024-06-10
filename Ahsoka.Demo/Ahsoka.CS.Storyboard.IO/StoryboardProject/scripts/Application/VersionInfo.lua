--Dependencies
local utils = require("Modules.Utilities")

local touchHardwareVersion
local touchFirmwareVersion
local bspVersion
local applicationVersion
local sdkVersion
local installerVersion


--booleans to turn off system init screen
local initialized = {hardware = false, firmware = false, bsp = false, app = false, sdk = false, installer = false}

--Create timer for ellipsis on init screen
local ellipsisTimer = gre.timer_set_interval(
  250,
  function() utils.AnimateEllipsis("VersionInfo.InitializingGroup.InitializingText.initString") end
)


---
--
function GetVersionInfo(mapargs)
  utils.ButtonHighlight("VersionInfo.GetStringsButton.pressAlpha")

  --Populate local variables
  FEE_SystemInfoRequest()
end

---
--
function VersionButtonReleased(mapargs)
  utils.ButtonDarken("VersionInfo.GetStringsButton.pressAlpha")
end


--Version Info Requests
--These functions update text on screen with the version info
---
--
function BEE_TouchHardwareVersion(mapargs)
  touchHardwareVersion = tostring(mapargs.context_event_data.touchHardwareVersion)
  gre.set_value("VersionInfo.VersionInfoTable.InfoText.1.1", touchHardwareVersion)
  
  --set hardware to true and potentially turn off initializing screen
  initialized.hardware = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"VersionInfo.InitializingGroup")
end

---
--
function BEE_TouchFirmwareVersion(mapargs)
  touchFirmwareVersion =  tostring(mapargs.context_event_data.touchFirmwareVersion)
  gre.set_value("VersionInfo.VersionInfoTable.InfoText.2.1", touchFirmwareVersion)
   
  --set firmware to true and potentially turn off initializing screen
  initialized.firmware = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"VersionInfo.InitializingGroup")
end

---
--
function BEE_BSPVersion(mapargs)
  bspVersion = tostring(mapargs.context_event_data.bspVersion)
  gre.set_value("VersionInfo.VersionInfoTable.InfoText.3.1", bspVersion) 
  
  --set bsp to true and potentially turn off initializing screen
  initialized.bsp = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"VersionInfo.InitializingGroup")
end

---
--
function BEE_ApplicationVersion(mapargs)
  applicationVersion = tostring(mapargs.context_event_data.applicationVersion)
  gre.set_value("VersionInfo.VersionInfoTable.InfoText.4.1", applicationVersion) 
  
  --set app to true and potentially turn off initializing screen
  initialized.app = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"VersionInfo.InitializingGroup")
end

---
--
function BEE_SDKVersion(mapargs)
  sdkVersion = tostring(mapargs.context_event_data.sdkVersion)
  gre.set_value("VersionInfo.VersionInfoTable.InfoText.5.1", sdkVersion) 
  
  --set sdk to true and potentially turn off initializing screen
  initialized.sdk = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"VersionInfo.InitializingGroup")
end

---
--
function BEE_InstallerVersion(mapargs)
  installerVersion = tostring(mapargs.context_event_data.installerVersion)
  gre.set_value("VersionInfo.VersionInfoTable.InfoText.6.1", installerVersion) 
  
  --set installer to true and potentially turn off initializing screen
  initialized.installer = true
  utils.MultiInitCheck(initialized,ellipsisTimer,"VersionInfo.InitializingGroup")
end
