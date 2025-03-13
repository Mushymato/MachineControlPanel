<frame layout={:SubitemLayout} padding="16" background={@Mods/StardewUI/Sprites/ControlBorder}>
  <scrollable peeking="128" scrollbar-margin="0,0,0,0">
    <grid item-layout="length: 76+" horizontal-item-alignment="middle">
      <panel *repeat={:SubItems} tooltip={:Tooltip} focusable="true">
        <image *repeat={:SpriteLayers} sprite={:Sprite} tint={:Tint} layout={:Layout} padding={:Padding}
          margin="6"
          focusable="true"
          +hover:scale="1.1"
          +transition:scale="100ms EaseInSine"/>
      </panel>
    </grid>
  </scrollable>
</frame>