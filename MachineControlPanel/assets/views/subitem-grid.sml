<frame layout={:SubitemLayout} border-thickness="16,16,16,22" border={@Mods/StardewUI/Sprites/ControlBorder}>
  <lane orientation="vertical" >
    <label text={:Header} layout="content 32px"/>
    <image sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="stretch 6px" margin="-4,2,-4,0" fit="Stretch"/>
    <scrollable peeking="128" scrollbar-margin="0,0,0,0" >
      <grid item-layout="length: 76+" horizontal-item-alignment="middle" padding="0,8,0,0">
        <panel *repeat={:SubItemsFiltered}
          tooltip={:Tooltip}
          pointer-enter=|^SetHover(this)|
          focusable="true">
          <image *repeat={:SpriteLayers} sprite={:Sprite} tint={:Tint} layout={:Layout} padding={:Padding} focusable="true"/>
        </panel>
      </grid>
    </scrollable>
  </lane>
</frame>