<lane orientation="vertical" horizontal-content-alignment="middle">
  <lane orientation="horizontal" vertical-content-alignment="middle">
    <panel>
      <image sprite={@mushymato.MachineControlPanel/sprites/cursors:magifyingGlass} layout="40px 40px" margin="8" tooltip={#machine-select.search}/>
    </panel>
    <textinput text={<>SearchText} background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="300px 60px" margin="0,14" text-color="#43111B" focusable="true"/>
    <include name="mushymato.MachineControlPanel/views/includes/global-toggle" *context={:GlobalToggle}/>
  </lane>
  <scrollable layout="75%[1152..] 90%[608..]" peeking="128" scrollbar-margin="0,0,0,0">
    <grid item-layout="length: 104+" horizontal-item-alignment="middle">
      <frame
        *repeat={MachineCells}
        background={@mushymato.MachineControlPanel/sprites/cursors:shopBg}
        background-tint={BackgroundTint}
        tooltip={:Tooltip}
        left-click=|ShowControlPanel()|
        focusable="true"
        padding="16,20,16,12"
        margin="4"
        >
        <image sprite={:MachineData}
          layout="64px 128px"
          fit="Contain"
          horizontal-alignment="middle"
          vertical-alignment="end"
          +hover:scale="1.1"
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
