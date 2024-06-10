
local profileExists = 0
local onPassword = 0
local gBackendChannel = "thermostat_backend"

function WiFiMoveUp(mapargs)
  gre.send_event_data (
    "wifi_message",
    "2u1 action 1s0 data", 
    {action = 1, data = ""}, 
    gBackendChannel)
  end
  
function WiFiMoveDown(mapargs)
  gre.send_event_data (
    "wifi_message",
    "2u1 action 1s0 data", 
    {action = 2, data = ""}, 
    gBackendChannel)
  end
  
function WiFiDisconnect(mapargs)
  gre.send_event_data (
    "wifi_message",
    "2u1 action 1s0 data", 
    {action = 4, data = ""}, 
    gBackendChannel)
  end
  
function WiFiRefresh(mapargs)
  gre.send_event_data (
    "wifi_message",
    "2u1 action 1s0 data", 
    {action = 3, data = ""}, 
    gBackendChannel)
  end
  
  
function WiFiConnect(mapargs) 
  if (profileExists == 1) then
    gre.send_event_data (
    "wifi_message",
    "2u1 action 1s0 data", 
    {action = 0, data = ""}, 
    gBackendChannel)
  else 
    onPassword = 1
    gre.send_event ("input_password")
  end
end

function LeftPassword()
  onPassword = 0
end

function UpdateNetworkView(mapargs)
  local ev_data = mapargs.context_event_data
  
  local data ={}
  if (ev_data.ssid ~= nil) then    
    data["wifi_layer.network_view.network_name.network_description"] = ev_data.ssid    
  end
  if (ev_data.saved_profile ~= nil) then
    profileExists = ev_data.saved_profile
    if (ev_data.saved_profile == 1) then
      data["wifi_layer.network_view.network_profile.image"] = "images/save-64.png"
    else 
      data["wifi_layer.network_view.network_profile.image"] = ""
    end
  end
  if (ev_data.signal_level ~= nil) then
    data["wifi_layer.network_view.network_strength.image"] = GetWifiImage(ev_data.signal_level)
  end
  gre.set_data(data)
end

function GetWifiImage(signal_level)
  if (signal_level < 20) then
    return "images/wifi-empty.png"
  elseif (signal_level < 40) then
    return "images/wifi-1.png"
  elseif (signal_level < 60) then
    return "images/wifi-2.png"
   elseif (signal_level < 80) then
    return "images/wifi-3.png"
  else
    return "images/wifi-white.png"
  end
end


function UpdateConnectionStatus(mapargs)
  local ev_data = mapargs.context_event_data
  
  local data ={}
  if (ev_data.ssid ~= nil) then
    data["wifi_layer.connection_status.status_message"] = ev_data.ssid   
  else
    data["wifi_layer.connection_status.status_message"] = "No Network"
  end
  
  gre.set_data(data)
end
