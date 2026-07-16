<frame>
  <button *!if={OverlayEnabled} text={#overlay-toggle.show} tooltip={#overlay-toggle.description} left-click=|ShowOverlay()|/>
  <button *if={OverlayEnabled} text={#overlay-toggle.hide} tooltip={#overlay-toggle.description} left-click=|HideOverlay()|/>
</frame>
