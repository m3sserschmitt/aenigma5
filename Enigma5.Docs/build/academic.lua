--[[
  Pandoc Lua filter used by build-pdf.sh.

  The chapters are written in plain GitHub Markdown, which has no captions or abstract blocks.
  They use three conventions instead, which this filter turns into proper structure for the PDF:

  - a paragraph "**Table N.M:** text" directly followed by a table becomes the table's caption;
  - a paragraph "**Listing N.M:** text" directly followed by a code block becomes the caption of
    that code block (both are wrapped in a Div with class "listing");
  - a paragraph starting with "**Abstract.**" is wrapped in a Div with class "abstract".
]]

local function label_of(block, pattern)
  if block == nil or block.t ~= "Para" or #block.content == 0 then
    return nil
  end
  local first = block.content[1]
  if first.t ~= "Strong" then
    return nil
  end
  local label = pandoc.utils.stringify(first)
  if label:match(pattern) then
    return label
  end
  return nil
end

function Blocks(blocks)
  local result = pandoc.List()
  local i = 1
  while i <= #blocks do
    local block, next_block = blocks[i], blocks[i + 1]

    if label_of(block, "^Table %d+%.%d+:$") and next_block and next_block.t == "Table" then
      next_block.caption.long = pandoc.Blocks({ pandoc.Plain(block.content) })
      result:insert(next_block)
      i = i + 2
    elseif label_of(block, "^Listing %d+%.%d+:$") and next_block and next_block.t == "CodeBlock" then
      local caption = pandoc.Div({ pandoc.Para(block.content) }, pandoc.Attr("", { "caption" }))
      result:insert(pandoc.Div({ caption, next_block }, pandoc.Attr("", { "listing" })))
      i = i + 2
    elseif label_of(block, "^Abstract%.$") then
      result:insert(pandoc.Div({ block }, pandoc.Attr("", { "abstract" })))
      i = i + 1
    else
      result:insert(block)
      i = i + 1
    end
  end
  return result
end
