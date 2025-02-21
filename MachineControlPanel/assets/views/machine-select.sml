<lane orientation="vertical" horizontal-content-alignment="middle">
  <lane orientation="horizontal" vertical-content-alignment="middle">
    <panel left-click=|ToggleGlobalLocal()| focusable="true" margin="8">
      <image *!if={IsGlobal} sprite={@mushymato.MachineControlPanel/sprites/furniture:globe} layout="64px 64px" tint="#000000" tooltip={#machine-select.scope.current-location}/>
      <image *if={IsGlobal} sprite={@mushymato.MachineControlPanel/sprites/furniture:globe} layout="64px 64px" tooltip={#machine-select.scope.everywhere}/>
    </panel>
    <panel>
      <image sprite={@mushymato.MachineControlPanel/sprites/cursors:chatBoxEnd} layout="56px 56px" sprite-effects="FlipHorizontally"/>
      <image sprite={@mushymato.MachineControlPanel/sprites/cursors:magifyingGlass} layout="40px 40px" margin="8" tooltip={#machine-select.search}/>
    </panel>
    <textinput text={<>SearchText} background={@mushymato.MachineControlPanel/sprites/chatBox:input} layout="300px 56px" margin="0,14" text-color="White" focusable="true"/>
  </lane>
  <scrollable layout="75%[1152..] 90%[608..]" peeking="128" scrollbar-margin="0,0,0,0">
    <grid item-layout="length: 104+" horizontal-item-alignment="middle">
      <frame
        *repeat={MachineCells}
        background={@mushymato.MachineControlPanel/sprites/cursors:shopBg}
        background-tint={BackgroundTint}
        tooltip={:Tooltip}
        left-click=|ShowControlPanel(^IsGlobal)|
        focusable="true"
        padding="16,20,16,12"
        margin="4"
        >
        <image sprite={:MachineData}
          layout="64px 128px"
          fit="Contain"
          horizontal-alignment="middle"
          vertical-alignment="end"
          +hover:scale="1.14"
          +transition:scale="100ms EaseInSine"/>
      </frame>
      <frame
        *if={ShowHiddenCount}
        background={@mushymato.MachineControlPanel/sprites/cursors:shopBg}
        background-tint="#7f7f7f80"
        tooltip={#machine-select.hidden-by-progression-mode}
        focusable="true"
        padding="16,20,16,12"
        margin="4"
        layout="64px 128px"
        horizontal-content-alignment="middle"
        vertical-content-alignment="middle"
        >
          <label text={HiddenByProgressionCountLabel}/>
      </frame>
    </grid>
  </scrollable>
</lane>
