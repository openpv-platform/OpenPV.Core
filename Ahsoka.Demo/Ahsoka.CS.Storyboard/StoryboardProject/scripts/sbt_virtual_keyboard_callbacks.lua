--[[
Copyright 2016 Crank Software Inc.
All Rights Reserved.
For more information email info@cranksoftware.com
** FOR DEMO PURPOSES ONLY **
]]--
local VKI = require("sbt_virtual_keyboard")

---@field sbt_virtual_keyboard#VKI
virtualKeyboard = nil

local scaledDown = false

--- Emulates a keydown/keyup event
--
--@param ucs2Value A UCS2 codepoint value
--@param text The UTF8 text from which the USC2 codepoint was derived.
--@param modifiers The shift modifiers for the keyboard input.
--@param KVI The instance of the keyboard
local function CBKVI_KeyEmulation(code, text, modifiers, virtualKeyboard, channel)
  if(modifiers == nil) then
    modifiers = 0
  end
  
  local active_screen = gre.env("active_screen")
  
  local args = {}
  args["code"] = code
  args["modifiers"] = modifiers
  
  if(code < math.pow(2,16)) then --Less than 2 bytes
    args["key"] = code
  end
 
  if(code == 10) then
    local pass = gre.get_data("password_layer.password.text")
    gre.send_event_data (
    "wifi_message",
    "2u1 action 1s0 data", 
    {action = 0, data = pass["password_layer.password.text"]}, 
    "thermostat_backend")
    gre.send_event("password_entered")
  else
    if(channel == nil) then
      gre.send_event_data("gre.keydown", "4u1 code 2u1 key 2u1 modifiers", args)
      gre.send_event_data("gre.keyup", "4u1 code 2u1 key 2u1 modifiers", args)
    else
      gre.send_event_data("gre.keydown", "4u1 code 2u1 key 2u1 modifiers", args, channel)
      gre.send_event_data("gre.keyup", "4u1 code 2u1 key 2u1 modifiers", args, channel)
    end
   end
end

function CBVKI_Init(mapargs) 

  if(virtualKeyboard == nil) then
    virtualKeyboard = VKI.new("password_sc", "keyboard_layer", "template", "password_layer.password.text", 
                            "us-std", "fonts/CODE2000-AUTOHINTED.TTF", "")
    virtualKeyboard:SetEmulation(CBKVI_KeyEmulation)
    end
end
