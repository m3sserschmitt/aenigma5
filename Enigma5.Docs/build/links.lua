--[[
  Pandoc Lua filter used by build-pdf.sh.

  Each chapter is converted on its own, then all chapters are merged into one document. So
  that links keep working in the merged document, this filter (run once per chapter):

  - prefixes every identifier in the chapter with the chapter name, so identical headings in
    different chapters (for example "Overview") do not collide;
  - rewrites links within the chapter ("#anchor") to the prefixed identifier;
  - rewrites links to other chapters ("05-cryptography-and-onions.md#anchor") to internal links;
  - rewrites links to files outside Enigma5.Docs ("../API.md") to their GitHub URL.

  The chapter name is passed as metadata: -M chapter=05-cryptography-and-onions
]]

local repository_url = "https://github.com/m3sserschmitt/aenigma5/blob/main/"
local chapter = nil

local function prefixed(name, anchor)
  if anchor == nil or anchor == "" then
    return name
  end
  return name .. "--" .. anchor
end

local function rewrite_target(target)
  -- external links, e-mail addresses and the like are left untouched
  if target:match("^%a[%w+.-]*:") then
    return target
  end

  local path, anchor = target:match("^([^#]*)#?(.*)$")

  -- link within the current chapter
  if path == "" then
    return "#" .. prefixed(chapter, anchor)
  end

  -- link to another chapter or appendix of this documentation
  local other = path:match("^%./(.+)%.md$") or path:match("^([^/]+)%.md$")
  if other ~= nil and other ~= "README" then
    return "#" .. prefixed(other, anchor)
  end

  -- link to a file elsewhere in the repository
  local relative = path:match("^%.%./(.+)$")
  if relative ~= nil then
    return repository_url .. relative .. (anchor ~= "" and ("#" .. anchor) or "")
  end

  return target
end

local function prefix_identifier(element)
  if element.identifier ~= nil and element.identifier ~= "" then
    element.identifier = prefixed(chapter, element.identifier)
    return element
  end
end

function Pandoc(document)
  chapter = pandoc.utils.stringify(document.meta.chapter or "")
  if chapter == "" then
    error("links.lua: missing -M chapter=<name>")
  end

  document = document:walk({
    Header = prefix_identifier,
    Div = prefix_identifier,
    Span = prefix_identifier,
    Link = function(link)
      link.target = rewrite_target(link.target)
      return link
    end,
  })

  -- anchor for links that target the chapter as a whole: the chapter is wrapped in a Div, so
  -- the anchor starts on the same page as the chapter heading
  document.blocks = pandoc.Blocks({ pandoc.Div(document.blocks, pandoc.Attr(chapter)) })
  return document
end
