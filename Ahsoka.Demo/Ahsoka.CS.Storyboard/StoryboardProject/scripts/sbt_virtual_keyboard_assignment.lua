--[[
Copyright 2016 Crank Software Inc.
All Rights Reserved.
For more information email info@cranksoftware.com
** FOR DEMO PURPOSES ONLY **
]]--

---  - Lay out each keyboard in rows of sub-arrays. Each sub-array
--    represents one key.
--  
--   - Each sub-array consists of four slots described as follows:
--      example: ["a", "A", "\u00e1", "\u00c1"]
--  
--         a) Normal character
--         A) Character + Shift/Caps
--      \u00e1) Character + Alt/AltGr/AltLk
--      \u00c1) Character + Shift/Caps + Alt/AltGr/AltLk
--  
--    You may include sub-arrays which are fewer than four slots.
--    In these cases, the missing slots will be blanked when the
--    corresponding modifier key (Shift or AltGr) is pressed.
--  
--   - If the second slot of a sub-array matches one of the following
--    strings:
--      "Tab", "Caps", "Shift", "Enter", "Bksp",
--      "Alt" OR "AltGr", "AltLk"
--    then the function of the key will be the following,
--    respectively:
--      - Insert a tab
--      - Toggle Caps Lock (technically a Shift Lock)
--      - Next entered character will be the shifted character
--      - Insert a newline (textarea), or does nothing
--      - Delete the previous character
--      - Next entered character will be the alternate character
--      - Toggle Alt/AltGr Lock
--  
--    The first slot of this sub-array will be the text to display
--    on the corresponding key. This allows for easy localisation
--    of key names.
--  
--   - Layout dead keys (diacritic + letter) should be added as
--    property/value pairs of objects with hash keys equal to the
--    diacritic.  See the "this.VKI_deadkey" object below the layout
--    definitions.  In each property/value pair, the value is what
--    the diacritic would change the property name to.
--  
--   - Note that any characters beyond the normal ASCII set should be
--    entered in escaped Unicode format.  (eg \u00a3 = Pound symbol)
--    You can find Unicode values for characters here:
--    http://unicode.org/charts/
--  
--   - To remove a keyboard, just delete it, or comment it out of the
--    source code. If you decide to remove the US International
--    keyboard layout, make sure you change the default layout
--    (this.VKI_kt) above so it references an existing layout.

---Populate all the virtual keyboard layouts.
-- @param #map<#string,sbt_virtual_keyboard#VKI_layout> keyLayout
function AssignKeyboards(keyLayout) 
  keyLayout['us-std'] = {
   ['name']= "US Standard", ['keys']= {
    {{"`", "~"}, {"1", "!"}, {"2", "@"}, {"3", "#"}, {"4", "$"}, {"5", "%"}, {"6", "^"}, {"7", "&"}, {"8", "*"}, {"9", "("}, {"0", ")"}, {"-", "_"}, {"=", "+"}, {"Bksp", "Bksp"}},
    {{"Tab", "Tab"}, {"q", "Q"}, {"w", "W"}, {"e", "E"}, {"r", "R"}, {"t", "T"}, {"y", "Y"}, {"u", "U"}, {"i", "I"}, {"o", "O"}, {"p", "P"}, {"[", "{"}, {"]", "}"}, {"\\", "|"}},
    {{"Caps", "Caps"}, {"a", "A"}, {"s", "S"}, {"d", "D"}, {"f", "F"}, {"g", "G"}, {"h", "H"}, {"j", "J"}, {"k", "K"}, {"l", "L"}, {";", ":"}, {"'", '"'}, {"Enter", "Enter"}},
    {{"Shift", "Shift"}, {"z", "Z"}, {"x", "X"}, {"c", "C"}, {"v", "V"}, {"b", "B"}, {"n", "N"}, {"m", "M"}, {",", "<"}, {".", ">"}, {"/", "?"}, {"Shift", "Shift"}},
    {{" ", " "}}
   }, ['lang']= {"en-us"} };

  ConvertKeyboardToUTF8(keyLayout)
end

function ConvertKeyboardToUTF8(keyLayout)
  local keyboard, rowKey, keyDef
  
  for name,layout in pairs(keyLayout) do
    layout.name = JSUCS2StringReplace(layout.name)
   
    keyboard = layout.keys
    for rows=1,#keyboard do
      rowKeys = keyboard[rows]
      for keySlot=1,#rowKeys do
        keyDef = rowKeys[keySlot]
        for k=1,#keyDef do
          keyDef[k] = JSUCS2StringReplace(keyDef[k])
        end
      end
    end
  end
end

--- Lay out each dead key set as an object of property/value
--    pairs.  The rows below are wrapped so uppercase letters are
--    below their lowercase equivalents.
--  
--   - The property name is the letter pressed after the diacritic.
--   - The property value is the letter this key-combo will generate.
--  
--   - Note that if you have created a new keyboard layout and want
--    it included in the distributed script, PLEASE TELL ME if you
--    have added additional dead keys to the ones below.
function AssignDeadKeys(deadkeyTable)
  
end


--- Converts a UCS2 (16bit) value to a UTF8 character table for most code points 
-- of interest (ignores U+D7FF to U+DFFF or those above U+FFFF)
--
--@param ucs2 The 16bit UCS2 integer value
--@param char_table A table into which the individual decoded characters are inserted
--@return The number of characters inserted
local function UCS2ToUTF8(ucs2, char_table)
  local byte0, byte1, byte2
  
    if (ucs2 < 0x80) then
      table.insert(char_table, ucs2)
      return 1
    end
    
    if (ucs2 >= 0x80  and ucs2 < 0x800) then
        --byte0 = (ucs2 >> 6)   | 0xC0;
        byte0 = bit32.bor(bit32.rshift(ucs2, 6), 0xC0)
        --byte1 = (ucs2 & 0x3F) | 0x80;
        byte1 = bit32.bor(bit32.band(ucs2, 0x3F), 0x80)
        
        table.insert(char_table, byte0)
        table.insert(char_table, byte1)
        return 2;
    end
    
    if (ucs2 >= 0x800 and ucs2 < 0xFFFF) then
        --byte0 = ((ucs2 >> 12)       ) | 0xE0;
        byte0 = bit32.bor(bit32.rshift(ucs2, 12), 0xE0)
        --byte1 = ((ucs2 >> 6 ) & 0x3F) | 0x80;
        byte1 = bit32.bor(bit32.band(bit32.rshift(ucs2, 6), 0x3F), 0x80)
        --byte2 = ((ucs2      ) & 0x3F) | 0x80;
        byte2 = bit32.bor(bit32.band(ucs2, 0x3F), 0x80)
       
        table.insert(char_table, byte0)
        table.insert(char_table, byte1)
        table.insert(char_table, byte2)
        return 3;
    end
    
    return 0;
end

--- This function converts a UTF-8 encoded string into a Lua table with 
--- its corresponding unicode codepoints (UTF-32)
--- @param #string utf8str The Lua string containing UTF8 values
--- @return #table A table of UCS2 integer codepoints
function UTF8ToUCS2(utf8str) 
  local res, seq, val = {}, 0, nil
  for i = 1, #utf8str do
    local c = utf8str:byte(i)
    if seq == 0 then
      table.insert(res, val)
      seq = c < 0x80 and 1 or c < 0xE0 and 2 or c < 0xF0 and 3 or
            c < 0xF8 and 4 or --c < 0xFC and 5 or c < 0xFE and 6 or
          error("invalid UTF-8 character sequence")
      val = bit32.band(c, 2^(8-seq) - 1)
    else
      val = bit32.bor(bit32.lshift(val, 6), bit32.band(c, 0x3F))
    end
    seq = seq - 1
  end
  table.insert(res, val)
  table.insert(res, 0)
  return res
end

---Convert a UCS2 string to UTF-8.  The UCS2 strings are a bit funky in that
-- they are not really encoded but "\u01D6" which turns into "u01D6" since
-- the \u isn't an escape character in Lua. 
-- @param #string ucs2 the UCS2 string to decode
-- @return 
function JSUCS2StringDecode(ucs2)
  if(ucs2 == nil) then
    return nil
  end
  
--  local ucs2dec = string.match(ucs2,"\\u(%d)+")
  local ucs2hex = string.match(ucs2,"^u(%x%x%x%x)$")
  if(ucs2hex == nil) then
    --print("No match on " .. tostring(ucs2))
    return ucs2
  end
  
  local utf8Chars = {}
  local cnt = UCS2ToUTF8(tonumber(ucs2hex, 16), utf8Chars)
  local str = string.char(unpack(utf8Chars))
  
  --print(string.format("UCS2 Number Found %s => %s", ucs2hex, str))
  
  return str
end

function JSUCS2StringReplace(ucs2String)
  return string.gsub(ucs2String, "(u%x%x%x%x)", JSUCS2StringDecode)
end

function UTF8Delete(utf8String)
  local len = #utf8String
  if(len <= 1) then
    return ""
  end
  
  -- Simple non-encoded character (no top bit set)
  --print(string.format("Delete %x", utf8String:byte(len)))
  if(bit32.band(utf8String:byte(len), 0x80) == 0) then
    return string.sub(utf8String, 1, len-1)
  end
  
  -- Trim off the 10xxxxxx bits indicating UTF8 encoding
  while(len > 1 and (bit32.band(utf8String:byte(len), 0xc0) == 0x80)) do
    --print(string.format("Ex Delete %x", utf8String:byte(len)))
    len = len - 1
  end
  
  -- Take one off for the final 11xxxxxx bits
  --print(string.format("Ex Last Delete %x", utf8String:byte(len)))
  len = len - 1
  
  if(len < 1) then
    return ""
  end
  
  return string.sub(utf8String, 1, len)
end