--Modules Table
local Utilities = {}


--Methods

---
--
function Utilities.ButtonHighlight(alphaPath)
  gre.set_value(alphaPath, 255)
end

---
--
function Utilities.ButtonDarken(alphaPath)
  gre.set_value(alphaPath, 0)
end

---
--
function Utilities.RoundToNearest(n, i) return math.floor(n/i+0.5)*i end
  --Takes number n and rounds it to the nearest i

---
--
function Utilities.AnimateEllipsis(currentString)
  --Takes "Initializing" and places the appropriate amount of .  
  local str = gre.get_value(currentString)
  if(str == "Initializing") then gre.set_value(currentString, "Initializing.")
  elseif(str == "Initializing.") then gre.set_value(currentString, "Initializing..")
  elseif(str == "Initializing..") then gre.set_value(currentString, "Initializing...")
  else gre.set_value(currentString, "Initializing")
  end
end

---
--
function Utilities.HideInitGroup(initGroupPath)
  --set up data variable to hide the init group
  local data = {}
  data.hidden = 1
  --hide current layer
  gre.set_group_attrs(initGroupPath, data)
end

---
--
local function CheckInitTable(table)
  --Checks if the init table only contains true.
  --This function is the check in MultiInitCheck to see if the initGroup should be hidden
  for i,v in pairs(table) do
    if(not v) then return false end
  end
  return true
end

---
--
function Utilities.MultiInitCheck(initTable, ellipsisTimer, initGroupPath)
  --If layer is fully initialized, hide init group
  if(CheckInitTable(initTable)) then 
    Utilities.HideInitGroup(initGroupPath)
    
    --If the ellipsis animation has not been stopped yet (do this by checking if the timer is not nil), stop it
    --This prevents the code from trying to stop a nil timer
    if(ellipsisTimer) then
      gre.timer_clear_interval(ellipsisTimer)
    end
  end
end

--Module Return
return Utilities
