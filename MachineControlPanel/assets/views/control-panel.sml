<lane orientation="vertical" horizontal-content-alignment="middle">
  <lane orientation="horizontal" vertical-content-alignment="middle">
    <image sprite={:MachineData}
      layout="48px 96px"
      margin="12,16,0,0"
      fit="Contain"
      horizontal-alignment="middle"
      vertical-alignment="middle"
      />
    <banner text={:MachineName}
      margin="16,24,0,0"
      background-border-thickness="48,16,48,14"
      background={@Mods/StardewUI/Sprites/BannerBackground}
      layout="content content"/>
    <panel margin="0,26,0,0">
      <include name="mushymato.MachineControlPanel/views/includes/global-toggle" *context={:GlobalToggle}/>
    </panel>
  </lane>
  <frame layout="1220px 70%[550..]"
    background={@Mods/StardewUI/Sprites/MenuBackground}
    border={@Mods/StardewUI/Sprites/MenuBorder}
    border-thickness="36, 36, 36, 36"
    *switch={PageIndex} >
    <lane *float="above" orientation="horizontal" vertical-content-alignment="end" margin="36,0,0,-24">
      <tab-label page="1" text={#rule-list.rules} margin={TabMarginRules} />
      <tab-label page="2" text={#rule-list.inputs} margin={TabMarginInputs}/>
    </lane>
    <label *case="1" text={#rule-list.rules}/>
    <label *case="2" text={#rule-list.inputs}/>
  </frame>
</lane>

<template name="tab-label">
  <frame
    background={@mushymato.MachineControlPanel/sprites/cursors:tabBg}
    padding="18,16,18,12"
    focusable="true"
    margin={&margin}
    left-click=|ChangePage(&page)|
    >
    <label text={&text}/>
  </frame>
</template>
