--Dependencies
local utils = require("Modules.Utilities")

---
--
function Shutdown(mapargs)
  utils.ButtonHighlight("ShutDown.Yes.Alpha")
  
  --Indicate shutdown request in console
  gre.log(gre.LOG_ALWAYS, "Sending shutdown request")
  
  --Send shutdown request  
  gre.send_event(
  "FEE_ShutdownRequest",
  BackendChannel
  )
end