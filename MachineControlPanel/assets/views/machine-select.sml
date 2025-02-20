<lane orientation="vertical" horizontal-content-alignment="middle">
  <lane orientation="horizontal" vertical-content-alignment="middle">
    <image sprite={@mushymato.MachineControlPanel/sprites/cursors:magifyingGlass} layout="60px 60px"/>
    <textinput text={<>SearchText} background={@mushymato.MachineControlPanel/sprites/chatBox:input} layout="300px 56px" margin="0,14" text-color="White" focusable="true"/>
    <image sprite={@mushymato.MachineControlPanel/sprites/cursors:organize} layout="64px 64px"/>
  </lane>
  <scrollable layout="75%[1152..] 90%[608..]"  peeking="128" scrollbar-margin="0,0,0,0">
    <grid item-layout="length: 104+" horizontal-item-alignment="middle">
      <frame
        *repeat={MachineCells}
        background-tint={BackgroundTint}
        background={@mushymato.MachineControlPanel/sprites/cursors:shopBg}
        tooltip={:Tooltip}
        focusable="true"
        padding="16,20,16,12"
        margin="4"
        >
        <image sprite={:ItemData}
          layout="64px 128px"
          fit="Contain"
          horizontal-alignment="middle"
          vertical-alignment="end"
          +hover:scale="1.14"
          +transition:scale="100ms EaseInSine"/>
      </frame>
      <frame
        *if={ShowHiddenCount}
        background-tint="#7f7f7f80"
        background={@mushymato.MachineControlPanel/sprites/cursors:shopBg}
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
