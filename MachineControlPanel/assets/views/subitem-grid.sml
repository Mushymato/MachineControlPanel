<frame layout={:SubitemLayout} padding="16" background={@Mods/StardewUI/Sprites/ControlBorder}>
  <scrollable peeking="128" scrollbar-margin="0,0,0,0">
    <grid item-layout="length: 76+" horizontal-item-alignment="middle">
      <image *repeat={:ItemDatas} sprite={:ItemData} tooltip={:Tooltip} layout="64px 64px" margin="6"/>
    </grid>
  </scrollable>
</frame>