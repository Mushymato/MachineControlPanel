<lane orientation="vertical" horizontal-content-alignment="middle" >
  <lane orientation="horizontal" vertical-content-alignment="middle">
    <include name="mushymato.MachineControlPanel/views/includes/global-toggle" *context={:GlobalToggle}/>
    <image sprite={@mushymato.MachineControlPanel/sprites/cursors:magifyingGlass} layout="40px 40px" margin="8"/>
    <textinput text={<>SearchText} placeholder={#rule-list.search} background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="240px 60px" margin="0,14" text-color="#43111B" focusable="true"/>
  </lane>
  <scrollable layout="75%[1152..] 90%[608..]" peeking="128" scrollbar-margin="0,0,0,0">
    <grid pointer-leave=|SetHover()| item-layout="length: 104+" horizontal-item-alignment="middle">
      <frame
        *repeat={MachineCellsFiltered}
        background={@mushymato.MachineControlPanel/sprites/cursors:shopBg}
        background-tint={BackgroundTint}
        tooltip={:Tooltip}
        pointer-enter=|^SetHover(this)|
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
